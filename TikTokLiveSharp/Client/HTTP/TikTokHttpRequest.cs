using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using TikTokLiveSharp.Client.Config;

namespace TikTokLiveSharp.Client.HTTP
{
    /// <summary>
    /// HTTP-Request for TikTok-Page
    /// </summary>
    public class TikTokHttpRequest : ITikTokHttpRequest
    {
        #region Properties
        #region Static
        #region Public
        /// <summary>
        /// Cookies for Requests
        /// </summary>
        public static TikTokCookieJar CookieJar { get; private set; }

        /// <summary>
        /// (Default) Headers for Requests
        /// </summary>
        public static HttpRequestHeaders CurrentHeaders => client?.DefaultRequestHeaders;

        /// <summary>
        /// AcceptLanguage-override for Requests
        /// <para>
        /// Quality for this is set to 0.9
        /// </para>
        /// </summary>
        public static string ClientLanguage
        {
            get => clientLanguage;
            set
            {
                if (string.Equals(clientLanguage, value))
                    return;
                if (client != null)
                    throw new Exception("ClientLanguage cannot be set after client has been initialized.");
                clientLanguage = value;
            }
        }

        /// <summary>
        /// Timeout used for HttpRequests
        /// </summary>
        public static TimeSpan Timeout
        {
            get => timeout;
            set
            {
                if (timeout == value)
                    return;
                if (client != null)
                    throw new Exception("Timeout cannot be set after client has been initialized.");
                timeout = value;
            }
        }

        /// <summary>
        /// WebProxy used for HttpRequests
        /// </summary>
        public static IWebProxy WebProxy
        {
            get => webProxy;
            set
            {
                if (webProxy == value)
                    return;
                if (client != null)
                    throw new Exception("WebProxy cannot be set after client has been initialized.");
                webProxy = value;
            }
        }
        #endregion

        #region Private
        /// <summary>
        /// Client used for HttpRequests
        /// </summary>
        private static volatile HttpClient client;

        /// <summary>
        /// Lock guarding the lazy initialization of <see cref="client"/>
        /// </summary>
        private static readonly object clientInitLock = new object();

        /// <summary>
        /// Timeout for Requests
        /// </summary>
        private static TimeSpan timeout;

        /// <summary>
        /// Proxy for Requests
        /// </summary>
        private static IWebProxy webProxy;

        /// <summary>
        /// AcceptLanguage-override for Requests
        /// <para>
        /// Quality for this is set to 0.9
        /// </para>
        /// </summary>
        private static string clientLanguage;
        #endregion
        #endregion

        #region Instance
        /// <summary>
        /// Query for this Request
        /// </summary>
        private string query;

        /// <summary>
        /// HTTP-Message for this Request
        /// </summary>
        private readonly HttpRequestMessage request;

        /// <summary>
        /// Whether this Request has been sent off
        /// </summary>
        private bool sent;
        #endregion
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a TikTok HTTPRequest
        /// </summary>
        /// <param name="url">The url to send the request to</param>
        /// <param name="useCookies">Whether to allow for Cookies on this HTTP-Request</param>
        /// <param name="enableCompression">Enable Compression for Http-Response</param>
        /// <exception cref="ArgumentException">Throws exception if URL is invalid</exception>
        public TikTokHttpRequest(string url, bool enableCompression = true, bool useCookies = true)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri result)) 
                throw new ArgumentException("Invalid Url", nameof(url));
            if (CookieJar == null)
                CookieJar = new TikTokCookieJar();
            EnsureClientInitialized(enableCompression, useCookies);
            request = new HttpRequestMessage
            {
                RequestUri = result
            };
            request.Headers.Host = result.Host;
            sent = false;
        }

        /// <summary>
        /// Lazily initializes the shared <see cref="client"/> in a thread-safe manner.
        /// </summary>
        /// <param name="enableCompression">Enable Compression for Http-Response</param>
        /// <param name="useCookies">Whether to allow for Cookies on the Http-Request</param>
        /// <remarks>
        /// Uses double-checked locking so the static client is created exactly once,
        /// even when multiple clients connect concurrently.
        /// <para>
        /// The default request-headers are also fully parsed here, while still single-threaded:
        /// <see cref="HttpRequestHeaders"/> parses raw header-strings lazily on first read, mutating the
        /// shared header-store. Without this, concurrent <see cref="HttpClient.SendAsync(HttpRequestMessage)"/>
        /// calls would parse them simultaneously, throwing a NullReferenceException while writing the headers.
        /// </para>
        /// </remarks>
        private static void EnsureClientInitialized(bool enableCompression, bool useCookies)
        {
            if (client != null)
                return;
            lock (clientInitLock)
            {
                if (client != null)
                    return;
                HttpClientHandler handler = new HttpClientHandler
                {
                    AutomaticDecompression = enableCompression ? ~DecompressionMethods.None : DecompressionMethods.None,
                    Proxy = WebProxy,
                    UseProxy = WebProxy != null,
                    UseCookies = useCookies
                };
                HttpClient newClient = new HttpClient(handler)
                {
                    Timeout = Timeout
                };
                foreach (KeyValuePair<string, string> header in Constants.DEFAULT_HTTP_HEADERS)
                    newClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                if (enableCompression)
                    newClient.DefaultRequestHeaders.Add(Constants.COMPRESSION_HEADER.Key, Constants.COMPRESSION_HEADER.Value);
                if (!string.IsNullOrEmpty(clientLanguage))
                    newClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue(clientLanguage, 0.9));
                // Force the lazy header-parsing to run once here, while still single-threaded.
                // After this, concurrent reads of DefaultRequestHeaders no longer mutate the shared store.
                _ = newClient.DefaultRequestHeaders.ToString();
                // Publish the fully-initialized client last (volatile write) so other threads never observe a partial instance.
                client = newClient;
            }
        }
        #endregion

        /// <summary>
        /// Sends a Get request
        /// </summary>
        /// <returns>HttpContent returned by request</returns>
        /// <exception cref="Exception">Thrown if Request has already been sent. Requests should not be reused</exception>
        public async Task<HttpContent> Get()
        {
            if (sent) 
                throw new InvalidOperationException("Requests should not be reused");
            request.Method = HttpMethod.Get;
            return await GetContent();
        }

        /// <summary>
        /// Sends a Post request
        /// </summary>
        /// <param name="data">The data to be sent.</param>
        /// <returns>HttpContent returned by request</returns>
        /// <exception cref="Exception">Thrown if Request has already been sent. Requests should not be reused</exception>
        public async Task<HttpContent> Post(HttpContent data)
        {
            if (sent) 
                throw new InvalidOperationException("Requests should not be reused");
            request.Method = HttpMethod.Post;
            request.Content = data; 
            return await GetContent();
        }

        /// <summary>
        /// Sets the queries for the request
        /// </summary>
        /// <param name="queries">Queries to append to the URL</param>
        /// <returns>Request with queries added</returns>
        public ITikTokHttpRequest SetQueries(IDictionary<string, object> queries)
        {
            if (queries == null) 
                return this;
            query = string.Join("&", queries.Select(x => $"{x.Key}={WebUtility.UrlEncode(x.Value.ToString())}"));
            return this;
        }

        /// <summary>
        /// Sends an async get request
        /// </summary>
        /// <returns>HttpResponse-Message (including Headers)</returns>
        /// <exception cref="Exception">Requests should not be reused</exception>
        public async Task<HttpResponseMessage> GetResponse()
        {
            request.Method = HttpMethod.Get;
            return await GetResponseMessage();
        }

        /// <summary>
        /// Sends Request and returns Response.
        /// </summary>
        /// <returns>Response for Request</returns>
        /// <exception cref="InvalidOperationException">Requests should not be reused</exception>
        private async Task<HttpResponseMessage> GetResponseMessage()
        {
            if (sent)
                throw new InvalidOperationException("Requests should not be reused");
            if (query != null)
                request.RequestUri = new Uri($"{request.RequestUri.AbsoluteUri}?{query}");
            HttpResponseMessage response = await client.SendAsync(request);
            request.Dispose();
            sent = true;
            return response;
        }

        /// <summary>
        /// Sends off this request
        /// </summary>
        /// <returns>HttpContent for Response</returns>
        /// <exception cref="HttpRequestException">If the request was unsuccessful</exception>
        private async Task<HttpContent> GetContent()
        {
            HttpResponseMessage response = await GetResponseMessage();
            if (response.StatusCode == HttpStatusCode.NotFound)
                throw new HttpRequestException("Request responded with 404 NOT_FOUND");
            if (response.StatusCode == HttpStatusCode.Forbidden)
                throw new HttpRequestException("Request responded with 403 FORBIDDEN");
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Request was unsuccessful [{(int)response.StatusCode}]");
            MediaTypeHeaderValue ct = response.Content.Headers?.ContentType;
            if (ct?.CharSet != null)
                ct.CharSet = ct.CharSet.Replace("\"", "");
            response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string> cookieValues);
            if (cookieValues == null) 
                return response.Content;
            foreach (string val in cookieValues)
            {
                string[] cookie = val.Split(';')[0].Split('=');
                CookieJar[cookie[0]] = cookie[1];
            }
            return response.Content;
        }
    }
}