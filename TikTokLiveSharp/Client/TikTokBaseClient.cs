using Newtonsoft.Json.Linq;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TikTokLiveSharp.Client.Config;
using TikTokLiveSharp.Client.HTTP;
using TikTokLiveSharp.Client.Socket;
using TikTokLiveSharp.Debugging;
using TikTokLiveSharp.Errors.Connections;
using TikTokLiveSharp.Errors.FetchErrors;
using TikTokLiveSharp.Errors.Messaging;
using TikTokLiveSharp.Models.HTTP;
using TikTokLiveSharp.Models.Protobuf.Messages;
using TikTokLiveSharp.Models.Protobuf.Messages.Generic;

namespace TikTokLiveSharp.Client
{
    /// <summary>
    /// Base-Client for TikTokLive. Handles Connections, Fetching of initial Info & Messaging
    /// </summary>
    public abstract class TikTokBaseClient
    {
        #region Events
        /// <summary>
        /// Event fired if an Operation threw an Exception
        /// <para>
        /// Used to ensure Exceptions can be handled even if a Thread crashes
        /// </para>
        /// </summary>
        public event EventHandler<Exception> OnException;
        #endregion

        #region Properties
        #region Public
        /// <summary>
        /// Available Gifts for Room
        /// </summary>
        public IReadOnlyDictionary<long, TikTokGiftData> AvailableGifts => new ReadOnlyDictionary<long, TikTokGiftData>(availableGifts);
        /// <summary>
        /// Gifts Displayed for Room
        /// <para>
        /// Does not include gifts considered "Exclusive"
        /// </para>
        /// </summary>
        public IReadOnlyDictionary<long, TikTokGiftData> DisplayedGifts => new ReadOnlyDictionary<long, TikTokGiftData>(availableGifts.Where(kvp => kvp.Value.Is_Displayed_On_Panel).ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
        /// <summary>
        /// Whether currently Connected to the TikTokServers
        /// </summary>
        public bool Connected => socketClient?.IsConnected ?? false;
        /// <summary>
        /// Whether currently Connecting to the TikTokServers
        /// </summary>
        public bool Connecting { get; protected set; }
        /// <summary>
        /// RoomID for Connected Room
        /// </summary>
        public string RoomID { get; protected set; }
        /// <summary>
        /// RoomInfo for Connected Room
        /// </summary>
        public JObject RoomInfo { get; protected set; }
        /// <summary>
        /// UserName of Host for Connected Room
        /// </summary>
        public string HostName { get; protected set; }
        /// <summary>
        /// Number of Viewers in Connected Room
        /// </summary>
        public long? ViewerCount { get; protected set; }
        #endregion

        #region Protected
        /// <summary>
        /// Settings for this Client
        /// </summary>
        protected ClientSettings settings;
        #endregion

        #region Private
        /// <summary>
        /// HTTP-Client
        /// </summary>
        private readonly TikTokHttpClient httpClient;
        /// <summary>
        /// Token used to Cancel this Client
        /// </summary>
        private CancellationToken token;
        /// <summary>
        /// Running Task(s) for this Client
        /// </summary>
        private Task runningTask, pollingTask;
        /// <summary>
        /// Url that socket is connected to
        /// </summary>
        private string connectedSocketUrl;
        /// <summary>
        /// Available Gifts for Room
        /// </summary>
        private readonly Dictionary<long, TikTokGiftData> availableGifts;
        /// <summary>
        /// Additional Parameters for HTTP-Client
        /// </summary>
        private readonly Dictionary<string, object> clientParams;
        /// <summary>
        /// WebSocket-Client
        /// </summary>
        private TikTokWebSocket socketClient;
        /// <summary>
        /// ConnectionData for WebSocketClient retrieved from Signing-Server
        /// </summary>
        private TikTokWebSocketConnectionData? connectionData;
        #endregion
        #endregion

        #region Constructors
        /// <summary>
        /// Constructor for a TikTokBaseClient
        /// </summary>
        /// <param name="hostId">ID for Host of to Connect to (@-name, without the @-symbol)</param>
        /// <param name="roomId">ID for Room to Connect to. Overrides HostId & skips scraping webpage</param>
        /// <param name="settings">Settings for Client</param>
        /// <param name="clientParams">Additional Parameters for HTTP-Client</param>
        protected TikTokBaseClient(string hostId, string roomId = null, ClientSettings? settings = null, Dictionary<string, object> clientParams = null)
        {
            if (string.IsNullOrEmpty(hostId) && string.IsNullOrEmpty(roomId))
                throw new ArgumentNullException(nameof(hostId), "Either HostId or RoomId is required.");
            HostName = hostId;
            RoomID = roomId;
            if (!settings.HasValue)
            {
                Debug.Log("Using Default Settings");
                settings = Constants.DEFAULT_SETTINGS;
            }
            this.settings = settings.Value;
            CheckSettings();
            RoomInfo = null;
            availableGifts = new Dictionary<long, TikTokGiftData>();
            ViewerCount = null;
            Connecting = false;
            this.clientParams = new Dictionary<string, object>();
            // Set default params
            foreach (KeyValuePair<string, object> parameter in Constants.DEFAULT_CLIENT_PARAMS)
                this.clientParams.Add(parameter.Key, parameter.Value);
            // Override using ClientSettings
            this.clientParams["app_language"] = this.settings.ClientLanguage;
            this.clientParams["webcast_language"] = this.settings.ClientLanguage;
            // Override using custom params
            if (clientParams != null)
                foreach (KeyValuePair<string, object> param in clientParams)
                    this.clientParams[param.Key] = param.Value;
            httpClient = new TikTokHttpClient(TimeSpan.FromSeconds(this.settings.Timeout), this.settings.EnableCompression, this.settings.Proxy, this.settings.ClientLanguage);
        }
        
        /// <summary>
        /// Constructor for a TikTokBaseClient
        /// </summary>
        /// <param name="uniqueId">Host-Name for Room to Connect to</param>
        /// <param name="timeout">Timeout for Connections</param>
        /// <param name="pollingInterval">Polling Interval for WebSocket-Connection</param>
        /// <param name="roomId">RoomId for Room to Connect to (overrides <paramref name="uniqueId"/> during connect)</param>
        /// <param name="enableCompression">Enable Compression for Http-Response</param>
        /// <param name="skipRoomInfo">Skip fetching RoomInfo</param>
        /// <param name="clientParams">Additional Parameters for HTTP-Client</param>
        /// <param name="processInitialData">Whether to process Data received when Connecting</param>
        /// <param name="enableExtendedGiftInfo">Whether to download List of Gifts on Connect</param>
        /// <param name="proxyHandler">Proxy for Connection</param>
        /// <param name="lang">ISO-Language for Client</param>
        /// <param name="socketBufferSize">BufferSize for WebSocket-Messages</param>
        /// <param name="logDebug">Whether to log messages to the Console</param>
        /// <param name="logLevel">LoggingLevel for debugging</param>
        /// <param name="printMessageData">Whether to print Base64-Data for Messages to Console</param>
        /// <param name="checkForUnparsedData">Whether to check Messages for Unparsed Data</param>
        protected TikTokBaseClient(string uniqueId,
            float? timeout = null,
            float? reconnectInterval = null,
            float? pollingInterval = null,
            string roomId = "",
            bool enableCompression = true,
            bool skipRoomInfo = false,
            Dictionary<string, object> clientParams = null,
            bool processInitialData = true,
            bool enableExtendedGiftInfo = true,
            IWebProxy proxyHandler = null,
            string lang = "en-US",
            uint socketBufferSize = 10_000,
            bool logDebug = true, 
            LogLevel logLevel = LogLevel.Error | LogLevel.Warning,
            bool printMessageData = false,
            bool checkForUnparsedData = false,
            string customSigningServer = null,
            string signingServerApiKey = null)
            : this(uniqueId, roomId,
                  new ClientSettings
                  {
                      Timeout = timeout ?? Constants.DEFAULT_TIMEOUT,
                      PollingInterval = pollingInterval ?? Constants.DEFAULT_POLLTIME,
                      ReconnectInterval = reconnectInterval ?? Constants.DEFAULT_RECONNECT_TIMEOUT,
                      EnableCompression = enableCompression,
                      SkipRoomInfo = skipRoomInfo,
                      HandleExistingMessagesOnConnect = processInitialData,
                      DownloadGiftInfo = enableExtendedGiftInfo,
                      Proxy = proxyHandler,
                      ClientLanguage = lang,
                      SocketBufferSize = socketBufferSize,
                      PrintToConsole = logDebug,
                      LogLevel = logLevel,
                      PrintMessageData = printMessageData,
                      CheckForUnparsedData = checkForUnparsedData,
                      CustomSigningServerUrl = customSigningServer,
                      SigningKey = signingServerApiKey
                  },
                  clientParams)
        { }

        /// <summary>
        /// Disconnects Socket when destroying Client
        /// </summary>
        ~TikTokBaseClient()
        {
            socketClient?.Disconnect();
        }

        /// <summary>
        /// Checks if ClientSettings are Valid
        /// </summary>
        private void CheckSettings()
        {
            ClientSettings s = settings;
            if (settings.Timeout.Equals(0))
                s.Timeout = Constants.DEFAULT_SETTINGS.Timeout;
            if (settings.ReconnectInterval.Equals(0))
                s.ReconnectInterval = Constants.DEFAULT_SETTINGS.ReconnectInterval;
            if (settings.PollingInterval.Equals(0))
                s.PollingInterval = Constants.DEFAULT_SETTINGS.PollingInterval;
            if (string.IsNullOrEmpty(settings.ClientLanguage))
                s.ClientLanguage = Constants.DEFAULT_SETTINGS.ClientLanguage;
            if (settings.SocketBufferSize < 500_000)
                s.SocketBufferSize = Constants.DEFAULT_SETTINGS.SocketBufferSize;
            settings = s;
        }
        #endregion

        #region Static
        /// <summary>
        /// Checks if a User exists on TikTok by attempting to get their Profile-Page
        /// </summary>
        /// <param name="userId">@-ID for User</param>
        /// <param name="enableCompression">Enable Compression for Http-Response</param>
        /// <param name="timeOut">TimeOut for HTTP-Connection (set NULL for default)</param>
        /// <param name="proxy">Proxy to use with HTTP-Client</param>
        /// <param name="queryParams">Additional Parameters for Query</param>
        /// <returns>True if User has a Profile-Page on TikTok</returns>
        public static async Task<bool> GetUserExists(string userId, bool enableCompression = true, TimeSpan? timeOut = null, IWebProxy proxy = null, IDictionary<string, object> queryParams = null)
        {
            if (timeOut == null)
                timeOut = TimeSpan.FromSeconds(Constants.DEFAULT_SETTINGS.Timeout);
            TikTokHttpClient tempClient = new TikTokHttpClient(timeOut.Value, enableCompression, proxy);
            try
            {
                await tempClient.GetProfilePage(userId, queryParams);
                return true;
            }
            catch (HttpRequestException ex)
            {
                if (ex.Message.Contains("NOT_FOUND"))
                    return false; // Profile returned 404.
                FailedFetchRoomInfoException exc =
                    new FailedFetchRoomInfoException(
                        "Failed to fetch html from ProfilePage, see stacktrace for more info.", ex);
                Debug.LogException(exc);
                return false;
            }
            catch (Exception e)
            {
                FailedFetchRoomInfoException exc =
                    new FailedFetchRoomInfoException(
                        "Failed to fetch html from ProfilePage, see stacktrace for more info.", e);
                Debug.LogException(exc);
                return false;
            }
        }

        /// <summary>
        /// Checks if a User is currently streaming by looking for a RoomId on their Live-Page
        /// </summary>
        /// <param name="userId">@-ID for User</param>
        /// <param name="enableCompression">Enable Compression for Http-Response</param>
        /// <param name="timeOut">TimeOut for HTTP-Connection (set NULL for default)</param>
        /// <param name="proxy">Proxy to use with HTTP-Client</param>
        /// <param name="queryParams">Additional Parameters for Query</param>
        /// <returns>True if User is currently streaming on TikTok</returns>
        public static async Task<bool> GetUserStreaming(string userId, bool enableCompression = true, TimeSpan? timeOut = null, IWebProxy proxy = null, IDictionary<string, object> queryParams = null)
        {
            if (timeOut == null)
                timeOut = TimeSpan.FromSeconds(Constants.DEFAULT_SETTINGS.Timeout);
            TikTokHttpClient tempClient = new TikTokHttpClient(timeOut.Value, enableCompression, proxy);
            string html;
            try
            {
                html = await tempClient.GetLivestreamPage(userId, queryParams);
            }
            catch (Exception e)
            {
                FailedFetchRoomInfoException exc = new FailedFetchRoomInfoException("Failed to fetch html from Livestream-Page, see stacktrace for more info.", e);
                Debug.LogException(exc);
                return false;
            }
            Match first = Regex.Match(html, "room_id=([0-9]*)");
            Match second = Regex.Match(html, "\"roomId\":\"([0-9]*)\"");
            if (first.Groups.Count >= 1 && first.Groups[1].Value != string.Empty)
                return true;
            return second.Groups.Count >= 1 && second.Groups[1].Value != string.Empty;
        }
        #endregion

        #region Connect
        /// <summary>
        /// Creates Threads for & Runs Connection with TikTokServers
        /// </summary>
        /// <param name="cancellationToken">Token used to Cancel Client</param>
        /// <param name="onConnectException">Callback for Errors during Exception</param>
        /// <param name="retryConnection">Whether to Retry connections that might be recoverable</param>
        public void Run(CancellationToken? cancellationToken = null, Action<Exception> onConnectException = null, bool retryConnection = false)
        {
            token = cancellationToken.HasValue ? cancellationToken.Value : new CancellationToken();
            token.ThrowIfCancellationRequested();
            if (ShouldLog(LogLevel.Information))
                Debug.Log("Starting Threads");
            Task<string> run = Task.Run(() => Start(token, onConnectException, retryConnection), token);
            run.Wait(cancellationToken.Value);
            runningTask.Wait(cancellationToken.Value);
            pollingTask.Wait(cancellationToken.Value);
        }

        /// <summary>
        /// Asynchronously Creates Threads for & Runs Connection with TikTokServers
        /// </summary>
        /// <param name="cancellationToken">Token used to Cancel Client</param>
        /// <param name="onConnectException">Callback for Errors during Exception</param>
        /// <param name="retryConnection">Whether to Retry connections that might be recoverable</param>
        public async Task RunAsync(CancellationToken? cancellationToken = null, Action<Exception> onConnectException = null, bool retryConnection = false)
        {
            token = cancellationToken.HasValue ? cancellationToken.Value : new CancellationToken();
            token.ThrowIfCancellationRequested();
            if (ShouldLog(LogLevel.Information))
                Debug.Log("Starting Threads");
            await Start(token, onConnectException, retryConnection);
            await runningTask;
            await pollingTask;
        }
        
        /// <summary>
        /// Starts Connection with TikTokServers
        /// </summary>
        /// <param name="cancellationToken">Token used to Cancel Client</param>
        /// <param name="onConnectException">Callback for Errors during Exception</param>
        /// <param name="retryConnection">Whether to Retry connections that might be recoverable</param>
        /// <exception cref="AlreadyConnectedException">Exception thrown if Already Connected</exception>
        /// <exception cref="AlreadyConnectingException">Exception thrown if Already Connecting</exception>
        /// <returns>Task to Await. Result is RoomID</returns>
        public async Task<string> Start(CancellationToken? cancellationToken = null, Action<Exception> onConnectException = null, bool retryConnection = false)
        {
            token = cancellationToken.HasValue ? cancellationToken.Value : new CancellationToken();
            try
            {
                token.ThrowIfCancellationRequested();
                if (ShouldLog(LogLevel.Information))
                    Debug.Log("Starting Connection");
                return await Connect(onConnectException);
            }
            catch (OperationCanceledException) // cancelled by User
            {
                if (ShouldLog(LogLevel.Warning))
                    Debug.LogWarning("Connect cancelled by User");
                return null;
            }
            catch (AConnectionException e)
            {
                if (ShouldLog(LogLevel.Error))
                    Debug.LogException(e);
                // Failed to Connect, but Host was Online
                if (e is FailedConnectionException && retryConnection)
                {
                    if (ShouldLog(LogLevel.Information))
                        Debug.Log("Retrying Connection");
                    Connecting = false;
                    await Task.Delay(TimeSpan.FromSeconds(settings.ReconnectInterval), cancellationToken.Value);
                    return await Start(cancellationToken, onConnectException, true);
                }
                if (e is FailedConnectionException)
                {
                    onConnectException?.Invoke(e);
                    throw e;
                }
                if (e is AlreadyConnectedException || e is AlreadyConnectingException)
                {
                    onConnectException?.Invoke(e); // Already Connected
                    return null;  // Exit Quietly
                }
                if (e is LiveNotFoundException)
                {
                    onConnectException?.Invoke(e); // LiveStream was not Found (or Host is not Online)
                    throw e;
                }
                throw;
            }
            catch (AFetchException e)
            {
                if (ShouldLog(LogLevel.Error))
                    Debug.LogException(e);
                onConnectException?.Invoke(e); // Failed to fetch critical Info for Connection
                throw;
            }
            catch (Exception e) // Other type of Exception
            {
                if (ShouldLog(LogLevel.Error))
                    Debug.LogException(e);
                onConnectException?.Invoke(e);
                throw;
            }
        }

        /// <summary>
        /// Connects to TikTok-Servers
        /// </summary>
        /// <param name="onConnectException">Callback for Exceptions thrown whilst Connecting</param>
        /// <returns>Task to Await. Result is RoomID</returns>
        /// <exception cref="AlreadyConnectingException">Exception thrown if Already Connecting</exception>
        /// <exception cref="AlreadyConnectedException">Exception thrown if Already Connected</exception>
        /// <exception cref="LiveNotFoundException">Exception thrown if Room could not be found for Host</exception>
        protected virtual async Task<string> Connect(Action<Exception> onConnectException = null)
        {
            if (Connecting)
                throw new AlreadyConnectingException();
            if (Connected)
                throw new AlreadyConnectedException();
            Connecting = true;
            if (!string.IsNullOrEmpty(RoomID))
            {
                if (ShouldLog(LogLevel.Verbose))
                    Debug.Log("Using provided RoomID");
                clientParams["room_id"] = RoomID;
            }
            else
            {
                if (ShouldLog(LogLevel.Verbose))
                    Debug.Log($"Fetching RoomID based on HostId {HostName}");
                RoomID = await FetchRoomId();
                if (ShouldLog(LogLevel.Verbose))
                    Debug.Log($"Found RoomId {RoomID} for Host {HostName}");
            }
            token.ThrowIfCancellationRequested();
            if (!settings.SkipRoomInfo)
            {
                if (ShouldLog(LogLevel.Verbose))
                    Debug.Log("Fetching RoomInfo");
                JObject info = await FetchRoomInfo();
                JToken status = info["data"]?["status"];
                if (status == null || status.Value<int>() == 4)
                    throw new LiveNotFoundException("LiveStream for HostID could not be found. Is the Host online?");
            }
            token.ThrowIfCancellationRequested();
            if (settings.DownloadGiftInfo)
            {
                try
                {
                    if (ShouldLog(LogLevel.Verbose))
                        Debug.Log("Fetching GiftInfo");
                    await FetchAvailableGifts();
                }
                catch (FailedFetchGiftsException e)
                {
                    if (ShouldLog(LogLevel.Error))
                        Debug.LogException(e);
                    onConnectException?.Invoke(e);
                    // Continue connecting (not a critical error)
                }
            }
            token.ThrowIfCancellationRequested();
            if (ShouldLog(LogLevel.Verbose))
                Debug.Log("Signing Connection");
            TikTokWebSocketConnectionData connectionData = default;
            try
            {
                connectionData = await httpClient.GetSignedWebsocketData(RoomID, settings.CustomSigningServerUrl, settings.SigningKey);
            }
            catch (Exception) {
                if (ShouldLog(LogLevel.Error))
                    Debug.LogError("[SIGNING-ERROR] An error occurred whilst signing the Socket-Url. If the issue persists, please contact the signing-server operator via the Discord.");
                Connecting = false;
                throw;
            }
            token.ThrowIfCancellationRequested();
            if (connectionData.InitialWebcastResponse == null) // Failed signing. We should exit/crash here.
            {
                Connecting = false;
                throw new SignConnectionException($"Failed signing connection to room {RoomID}, name {HostName}");
            }
            if (ShouldLog(LogLevel.Information))
                Debug.Log("Creating WebSocketClient");
            try
            {
                await CreateWebSocket(connectionData);
            }
            catch (HandleMessageException)
            {
                if (ShouldLog(LogLevel.Error))
                    Debug.LogError("[PARSE-ERROR] An error occurred whilst parsing the initial messages for the Livestream. However; the socket did connect.");
            }
            catch (Exception)
            {
                if (ShouldLog(LogLevel.Error))
                    Debug.LogError("[SOCKET-ERROR] An error occurred whilst connecting to the TikTok-Server's WebSocket.");
                Connecting = false;
                throw;
            }
            Connecting = false;
            token.ThrowIfCancellationRequested();
            return RoomID;
        }

        /// <summary>
        /// Creates WebSocket-Connection for Client
        /// </summary>
        /// <param name="response">Response with Client-Data</param>
        /// <returns>Task to await</returns>
        /// <exception cref="LiveNotFoundException">Thrown if Room could not be found (Invalid WebcastResponse)</exception>
        /// <exception cref="FailedConnectionException">Thrown if WebSocket could not Connect</exception>
        /// <exception cref="HandleMessageException">Thrown if an Error occurred during Parse of initial Messages</exception>
        private async Task CreateWebSocket(TikTokWebSocketConnectionData connectionData)
        {
            this.connectionData = connectionData;
            Response response = connectionData.InitialWebcastResponse;
            if (response == null || string.IsNullOrEmpty(response.PushServer) || response.RouteParamsMap == null)
                throw new LiveNotFoundException("Could not find Room");
            try
            {
                clientParams["room_id"] = connectionData.RoomId;
                clientParams["cursor"] = response.Cursor;
                clientParams["resp_content_type"] = "protobuf";
                clientParams["internal_ext"] = response.InternalExt;
                foreach (KeyValuePair<string, string> param in response.RouteParamsMap)
                {
                    if (ShouldLog(LogLevel.Verbose))
                        Debug.Log($"Adding Custom Param {param.Key}-{param.Value}");
                    clientParams[param.Key] = param.Value;
                }
                string version_code = response.PushServer.Contains("ws-useast1a") ? "270000" : "180800";
                string url = $"{response.PushServer}?version_code={version_code}&{string.Join("&", clientParams.Select(x => $"{x.Key}={WebUtility.UrlEncode(x.Value.ToString())}"))}";
                if (ShouldLog(LogLevel.Verbose))
                    Debug.Log($"Creating Socket with URL {url}");
                socketClient = new TikTokWebSocket(TikTokHttpRequest.CookieJar, settings.SocketBufferSize, connectionData.CookieHeaders, token, settings.Proxy);
                connectedSocketUrl = url;
                await socketClient.Connect(url);
                if (ShouldLog(LogLevel.Information))
                    Debug.Log($"Starting Socket-Threads");
                runningTask = Task.Run(WebSocketLoop, token);
                pollingTask = Task.Run(PingLoop, token);
            }
            catch (Exception e)
            {
                if (ShouldLog(LogLevel.Error))
                    Debug.LogException(e);
                connectedSocketUrl = null;
                this.connectionData = null;
                throw new FailedConnectionException("Failed to connect to the websocket", e);
            }
            if (settings.HandleExistingMessagesOnConnect)
            {
                try
                {
                    HandleMessages(response);
                }
                catch (Exception e)
                {
                    if (ShouldLog(LogLevel.Error))
                        Debug.LogException(e);
                    throw new HandleMessageException("Error Handling Initial Messages", e);
                }
            }
        }
        #endregion

        #region Disconnect
        /// <summary>
        /// Stops this Client
        /// </summary>
        /// <returns>Task to await</returns>
        public async Task Stop()
        {
            if (Connected)
            {
                try
                {
                    await Disconnect();
                }
                catch (WebSocketException) { }
            }
        }

        /// <summary>
        /// Disconnects 
        /// </summary>
        /// <returns>Task to await</returns>
        protected virtual async Task Disconnect()
        {
            RoomID = null;
            RoomInfo = null;
            Connecting = false;
            connectedSocketUrl = null;
            if (Connected)
            {
                if (ShouldLog(LogLevel.Information))
                    Debug.Log("Disconnecting SocketClient");
                await socketClient.Disconnect();
            }
            clientParams["cursor"] = string.Empty;
            await runningTask;
            await pollingTask;
        }
        #endregion

        #region FetchData
        /// <summary>
        /// Fetches RoomID for Host
        /// </summary>
        /// <returns></returns>
        /// <exception cref="FailedFetchRoomInfoException">Thrown if valid RoomID for Host could not be parsed</exception>
        private async Task<string> FetchRoomId(IDictionary<string, object> parameters = null, bool retryAttempt = false)
        {
            IDictionary<string, object> queryParams = clientParams ?? new Dictionary<string, object>();
            if (parameters != null)
                foreach (KeyValuePair<string, object> param in parameters)
                    queryParams[param.Key] = param.Value;
            string html;
            try
            {
                if (ShouldLog(LogLevel.Information))
                    Debug.Log("Scraping Webpage for Room-ID");
                html = await httpClient.GetLivestreamPage(HostName, queryParams);
            }
            catch (Exception e)
            {
                FailedFetchRoomInfoException exc = new FailedFetchRoomInfoException("Failed to scrape Webpage, see stacktrace for more info.", e);
                if (ShouldLog(LogLevel.Error))
                    Debug.LogException(exc);
                throw exc;
            }
            Match first = Regex.Match(html, "room_id=([0-9]*)");
            Match second = Regex.Match(html, "\"roomId\":\"([0-9]*)\"");
            string id = string.Empty;
            if (first.Groups.Count >= 1 && first.Groups[1].Value != string.Empty)
                id = first.Groups[1].Value;
            else if (second.Groups.Count >= 1 && second.Groups[1].Value != string.Empty)
                id = second.Groups[1].Value;
            if (!string.IsNullOrEmpty(id))
            {
                clientParams["room_id"] = id;
                RoomID = id;
                return id;
            }
            else
            {
                if (html.Contains("Please wait...") && !retryAttempt)
                {
                    if (ShouldLog(LogLevel.Information))
                        Debug.Log("TikTok returned 'Please Wait' instead of the page. Retrying.");
                    return await FetchRoomId(parameters, true);
                }
                else
                {
                    FailedFetchRoomInfoException exc = new FailedFetchRoomInfoException("Could not find RoomId on Webpage. " + (html.Contains("\"og:url\"") ? "User might be offline." : "Your IP or country might be blocked by TikTok."));
                    if (ShouldLog(LogLevel.Error))
                        Debug.LogException(exc);
                    throw exc;
                }
            }
        }

        /// <summary>
        /// Fetches List of available Gifts (for Room)
        /// </summary>
        /// <returns>Task to await. Result is Gifts by ID</returns>
        /// <exception cref="FailedFetchGiftsException">Thrown if Operation had an Error</exception>
        private async Task FetchAvailableGifts(IDictionary<string, object> parameters = null)
        {
            try
            {
                IDictionary<string, object> queryParams = clientParams ?? new Dictionary<string, object>();
                if (parameters != null)
                    foreach (KeyValuePair<string, object> param in parameters)
                        queryParams[param.Key] = param.Value;
                JObject response = await httpClient.GetJObjectFromWebcastApi("gift/list/", queryParams);
                JToken giftTokens = response["data"]?["gifts"];
                if (giftTokens == null)
                    return;
                foreach (JToken giftToken in giftTokens)
                {
                    TikTokGiftData gift = giftToken.ToObject<TikTokGiftData>();
                    if (ShouldLog(LogLevel.Verbose))
                        Debug.Log($"Found Available Gift {gift.Name} with ID {gift.Id}");
                    availableGifts[gift.Id] = gift;
                }
            }
            catch (Exception e)
            {
                FailedFetchGiftsException exc = new FailedFetchGiftsException("Failed to fetch giftTokens from WebCast, see stacktrace for more info.", e);
                if (ShouldLog(LogLevel.Error))
                    Debug.LogException(exc);
                throw exc;
            }
        }

        /// <summary>
        /// Fetches MetaData for Room
        /// </summary>
        /// <returns>Task to await. Result is JSON for RoomInfo</returns>
        /// <exception cref="FailedFetchRoomInfoException">Thrown if Operation had an Error</exception>
        private async Task<JObject> FetchRoomInfo(IDictionary<string, object> parameters = null)
        {
            try
            {
                IDictionary<string, object> queryParams = clientParams ?? new Dictionary<string, object>();
                if (parameters != null)
                    foreach (KeyValuePair<string, object> param in parameters)
                        queryParams[param.Key] = param.Value;
                JObject response = await httpClient.GetJObjectFromWebcastApi("room/info/", queryParams);
                RoomInfo = response;
                return response;
            }
            catch (Exception e)
            {
                FailedFetchRoomInfoException exc = new FailedFetchRoomInfoException("Failed to fetch room info from WebCast, see stacktrace for more info.", e);
                if (ShouldLog(LogLevel.Error))
                    Debug.LogException(exc);
                throw exc;
            }
        }
        #endregion

        #region SocketLoop
        /// <summary>
        /// Receives Messages from WebSocket. Sends back Acknowledgement for each
        /// </summary>
        /// <returns>Task to await</returns>
        /// <exception cref="WebSocketException">Thrown if WebSocket crashed with an Error</exception>
        private async Task WebSocketLoop()
        {
            while (!token.IsCancellationRequested && socketClient.IsConnected)
            {
                token.ThrowIfCancellationRequested();
                await CheckSocketConnection(); // Check SocketConnection
                token.ThrowIfCancellationRequested();
                TikTokWebSocketResponse response = await socketClient.ReceiveMessage();
                if (response == null) 
                    continue;
                try
                {
                    token.ThrowIfCancellationRequested();
                    using (MemoryStream websocketMessageStream = new MemoryStream(response.Array, 0, response.Count))
                    {
                        token.ThrowIfCancellationRequested();
                        PushFrame pushFrame = Serializer.Deserialize<PushFrame>(websocketMessageStream);
                        token.ThrowIfCancellationRequested();
                        if (pushFrame.Payload != null)
                        {
                            using (MemoryStream messageStream = new MemoryStream(pushFrame.Payload))
                            {
                                token.ThrowIfCancellationRequested();
                                Response message = Serializer.Deserialize<Response>(messageStream);
                                token.ThrowIfCancellationRequested();
                                if (message.NeedsAck)
                                    await SendAcknowledgement(pushFrame.SeqId);
                                token.ThrowIfCancellationRequested();
                                HandleMessages(message);
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    if (ShouldLog(LogLevel.Information))
                        Debug.LogWarning("User Closed Connection. Stopping WebSocketLoop.");
                    await Disconnect(); // Disconnect for PingLoop
                    return; // Stop this Loop (Cleanly)
                }
                catch (Exception e)
                {
                    Debug.LogError("Socket Crashed!");
                    Debug.LogException(e);
                    await Disconnect();
                    OnException?.Invoke(this, e); // Pass Exception to Controller
                    throw new WebSocketException("Websocket saw an Error and Closed",
                        e); // Crash this Thread (Violently)
                }
            }
        }

        /// <summary>
        /// Pings the websocket
        /// </summary>
        /// <returns>Task to await</returns>
        private async Task PingLoop()
        {
            int pollingMillis = (int)(settings.PollingInterval * 1000f);
            ArraySegment<byte> pingMessage = new ArraySegment<byte>(new byte[] { 58, 2, 104, 98 });
            while (socketClient?.IsConnected ?? false)
                try
                {
                    token.ThrowIfCancellationRequested();
                    await CheckSocketConnection(); // Check SocketConnection
                    token.ThrowIfCancellationRequested();
                    await socketClient.WriteMessage(pingMessage);
                    await Task.Delay(pollingMillis, token);
                }
                catch (TaskCanceledException)
                {
                    return; // Loop was ended. Clean exit.
                }
        }

        /// <summary>
        /// Send an acknowledgement to the websocket
        /// </summary>
        /// <param name="id">Acknowledgment id</param>
        /// <returns>Task to await</returns>
        private async Task SendAcknowledgement(ulong id)
        {
            token.ThrowIfCancellationRequested();
            if (!socketClient?.IsConnected ?? true)
                return; // Socket invalid (closed?)
            await CheckSocketConnection(); // Check SocketConnection
            token.ThrowIfCancellationRequested();
            using (MemoryStream messageStream = new MemoryStream())
            {
                Serializer.Serialize(messageStream, new WebsocketAck(id));
                await socketClient.WriteMessage(new ArraySegment<byte>(messageStream.ToArray()));
            }
        }
        #endregion

        #region Exceptions
        /// <summary>
        /// Calls OnException-Event in Base-Class
        /// </summary>
        /// <param name="exception">Exception for Event</param>
        protected void CallOnException(Exception exception)
        {
            OnException?.Invoke(this, exception);
        }
        #endregion

        /// <summary>
        /// Checks whether a LogMessage should be printed
        /// </summary>
        /// <param name="msgLevel">LogLevel for Message</param>
        /// <returns>True if Message should be printed to Console</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool ShouldLog(LogLevel msgLevel) => settings.PrintToConsole && settings.LogLevel.HasFlag(msgLevel);

        /// <summary>
        /// Handles Response received from TikTokServer
        /// </summary>
        /// <param name="response">The current response</param>
        protected abstract void HandleMessages(Response response);

        /// <summary>
        /// Checks if SocketConnection was Aborted, and reconnects if this was the case.
        /// </summary>
        /// <returns>Task to await</returns>
        private async Task CheckSocketConnection()
        {
            if (socketClient == null)
                return;
            if (socketClient.State == WebSocketState.Aborted)
            {
                if (ShouldLog(LogLevel.Information))
                    Debug.LogWarning("Reconnecting SocketClient");
                // Disconnect existing Client
                await socketClient.Disconnect();
                // Reconnect with new SocketClient
                Dictionary<string, string> customHeaders = new Dictionary<string, string>();
                if (connectionData.HasValue)
                {
                    StringBuilder cookieString = new StringBuilder();
                    foreach (KeyValuePair<string, string> cookie in connectionData.Value.CookieHeaders)
                    {
                        cookieString.Append($"{cookie.Key}={cookie.Value};");
                    }
                    customHeaders.Add("Cookie", cookieString.ToString());
                }
                socketClient = new TikTokWebSocket(TikTokHttpRequest.CookieJar, settings.SocketBufferSize, customHeaders, token, settings.Proxy);
                await socketClient.Connect(connectedSocketUrl);
            }
        }
    }
}