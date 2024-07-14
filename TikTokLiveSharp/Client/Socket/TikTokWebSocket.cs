using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TikTokLiveSharp.Client.Config;
using TikTokLiveSharp.Client.HTTP;

namespace TikTokLiveSharp.Client.Socket
{
    /// <summary>
    /// WebSocket-Connection to TikTok-servers
    /// </summary>
    public sealed class TikTokWebSocket : ITikTokWebSocket
    {
        #region Properties
        #region Public
        /// <summary>
        /// Is the websocket currently connected?
        /// </summary>
        public bool IsConnected => clientWebSocket != null && clientWebSocket.State == WebSocketState.Open;

        /// <summary>
        /// State for WebSocket
        /// </summary>
        public WebSocketState State => clientWebSocket?.State ?? WebSocketState.None;
        #endregion

        #region Private
        /// <summary>
        /// WebSocket-Client
        /// </summary>
        private ClientWebSocket clientWebSocket;

        /// <summary>
        /// Token used to Cancel this Connection
        /// </summary>
        private readonly CancellationToken token;

        /// <summary>
        /// Buffer used to store incoming Messages
        /// </summary>
        private readonly byte[] buffer;

        /// <summary>
        /// Buffer used to read from WebSocket
        /// </summary>
        private readonly byte[] readBuffer;

        /// <summary>
        /// Semaphore used to prevent concurrent writes
        /// </summary>
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
        #endregion
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a TikTok WebSocket instance
        /// </summary>
        /// <param name="cookieContainer">The cookie container to use</param>
        /// <param name="buffSize">Size for Message-Buffer</param>
        /// <param name="token">CancellationToken for the Socket-Connection</param>
        /// <param name="webProxy">Proxy to use for the Connection</param>
        public TikTokWebSocket(TikTokCookieJar cookieContainer, uint buffSize = 500_000, Dictionary<string, string> headers = null, CancellationToken? token = null, IWebProxy webProxy = null)
        {
            this.token = token.HasValue ? token.Value : CancellationToken.None;
            buffer = new byte[buffSize];
            readBuffer = new byte[buffSize];
            clientWebSocket = new ClientWebSocket();
            clientWebSocket.Options.Proxy = webProxy;
            clientWebSocket.Options.AddSubProtocol("echo-protocol");
            clientWebSocket.Options.KeepAliveInterval = TimeSpan.Zero;
            if (headers.ContainsKey("ttwid"))
                cookieContainer["ttwid"] = headers["ttwid"];
            Dictionary<string, string> combinedCookies = new Dictionary<string, string>(cookieContainer.Cookies);
            foreach (KeyValuePair<string, string> overrideCookie in headers)
            {
                combinedCookies[overrideCookie.Key] = overrideCookie.Value;
            }
            StringBuilder cookieHeader = new StringBuilder(cookieContainer.Count * 20);
            foreach (KeyValuePair<string, string> cookie in combinedCookies)
                cookieHeader.Append($"{cookie.Key}={cookie.Value};");
            foreach (KeyValuePair<string, string> header in Constants.DEFAULT_SOCKET_HEADERS)
                clientWebSocket.Options.SetRequestHeader(header.Key, header.Value);
            clientWebSocket.Options.SetRequestHeader("Cookie", cookieHeader.ToString());
        }
        #endregion

        #region Methods
        /// <summary>
        /// Connects to the websocket
        /// </summary>
        /// <param name="url">Websocket url</param>
        /// <returns>Task to await</returns>
        public async Task Connect(string url)
        {
            token.ThrowIfCancellationRequested();
            await clientWebSocket.ConnectAsync(new Uri(url), token);
        }

        /// <summary>
        /// Disconnects from the websocket
        /// </summary>
        /// <returns>Task to await</returns>
        public async Task Disconnect()
        {
            try
            {
                token.ThrowIfCancellationRequested();
                await clientWebSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, token);
            }
            catch (OperationCanceledException)
            { } // Do Not Throw this. Clean exit
            finally
            {
                clientWebSocket = null;
            }
        }

        /// <summary>
        /// Writes a message to the websocket
        /// </summary>
        /// <param name="arr">The bytes to write</param>
        /// <returns>Task to await</returns>
        public async Task WriteMessage(ArraySegment<byte> arr)
        {
            await semaphore.WaitAsync(token);
            try
            {
                token.ThrowIfCancellationRequested();
                if (clientWebSocket == null)
                    return;
                await clientWebSocket.SendAsync(arr, WebSocketMessageType.Binary, true, token);
            }
            finally
            {
                semaphore.Release();
            }
        }
        /// <summary>
        /// Receives a message from websocket. Result is Response-Message from Socket
        /// </summary>
        /// <returns>Task to Await</returns>
        public async Task<TikTokWebSocketResponse> ReceiveMessage()
        {
            token.ThrowIfCancellationRequested();
            if (clientWebSocket == null)
                return null; // Socket was Closed
            // Read Socket & Combine Messages until EndOfMessage-Flag is set
            bool endOfMessage = false;
            bool valid = true;
            List<byte> message = new List<byte>(buffer.Length);
            while (!endOfMessage)
            {
                token.ThrowIfCancellationRequested();
                ArraySegment<byte> arr = new ArraySegment<byte>(readBuffer);
                WebSocketReceiveResult response = await clientWebSocket.ReceiveAsync(arr, token);
                if (response.MessageType != WebSocketMessageType.Binary || arr.Array == null)
                    valid = false;
                else
                    message.AddRange(arr.Take(response.Count));
                endOfMessage = response.EndOfMessage;
            }
            token.ThrowIfCancellationRequested();
            return valid ? new TikTokWebSocketResponse(message.ToArray(), message.Count) : null;
        }
        #endregion
    }
}
