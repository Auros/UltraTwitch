using System;
using System.IO;
using System.Net;
using System.Text;
using System.Net.Http;
using Newtonsoft.Json;
using System.Threading;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Collections.Generic;


// Borrowed from Versus, thanks nate!
namespace UltraTwitch
{
    internal class WebResponse
    {
        public readonly HttpStatusCode StatusCode;
        public readonly string ReasonPhrase;
        public readonly HttpResponseHeaders Headers;
        public readonly HttpRequestMessage RequestMessage;
        public readonly bool IsSuccessStatusCode;

        private readonly byte[] _content;

        internal WebResponse(HttpResponseMessage resp, byte[] content)
        {
            StatusCode = resp.StatusCode;
            ReasonPhrase = resp.ReasonPhrase;
            Headers = resp.Headers;
            RequestMessage = resp.RequestMessage;
            IsSuccessStatusCode = resp.IsSuccessStatusCode;

            _content = content;
        }

        public byte[] ContentToBytes() => _content;
        public string ContentToString() => Encoding.UTF8.GetString(_content);
        public T ContentToJson<T>()
        {
            return JsonConvert.DeserializeObject<T>(ContentToString());
        }
        public JObject ConvertToJObject()
        {
            return JObject.Parse(ContentToString());
        }
    }

    internal class WebClient
    {
        private readonly HttpClient _client;

        internal WebClient()
        {
            _client = new HttpClient();
            _client.DefaultRequestHeaders.UserAgent.TryParseAdd($"UltraTwitch/{Plugin.Version}");
        }

        ~WebClient()
        {
            if (_client != null)
            {
                _client.Dispose();
            }
        }

        internal async Task<WebResponse> PostAsync(string url, object postData, CancellationToken token, AuthenticationHeaderValue authHeader = null)
        {
            return await SendAsync(HttpMethod.Post, url, token, postData, authHeader);
        }

        internal async Task<WebResponse> GetAsync(string url, CancellationToken token, AuthenticationHeaderValue authHeader = null)
        {
            return await SendAsync(HttpMethod.Get, url, token, authHeader: authHeader);
        }

        internal async Task<byte[]> DownloadImage(string url, CancellationToken token, AuthenticationHeaderValue authHeader = null)
        {
            var response = await SendAsync(HttpMethod.Get, url, token, authHeader: authHeader);

            if (response.IsSuccessStatusCode)
            {
                return response.ContentToBytes();
            }
            return null;
        }

        internal async Task<WebResponse> GetTwitchAsync(string url, CancellationToken token, string oauth, string clientID)
        {
            var header = new List<Tuple<string, string>>
            {
                new Tuple<string, string>("Client-ID", clientID)
            };
            return await SendAsync(HttpMethod.Get, url, token, null, new AuthenticationHeaderValue("Bearer", oauth), null, header);
        }

        internal async Task<WebResponse> SendAsync(HttpMethod methodType, string url, CancellationToken token, object postData = null, AuthenticationHeaderValue authHeader = null, IProgress<double> progress = null, List<Tuple<string, string>> headers = null)
        {
            Plugin.Log.Debug("Sending web request");
            Plugin.Log.Debug($"{methodType}: {url}");

            // create new request messsage
            var req = new HttpRequestMessage(methodType, url);

            // add authorization header
            req.Headers.Authorization = authHeader;

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    req.Headers.Add(header.Item1, header.Item2);
                }
            }

            // add json content, if provided
            if (methodType == HttpMethod.Post && postData != null)
            {
                req.Content = new StringContent(JsonConvert.SerializeObject(postData), Encoding.UTF8, "application/json");
            }
            // send request
            var resp = await _client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);

            //if ((int)resp.StatusCode == 429)
            //{
            //    // rate limiting handling
            //}

            if (token.IsCancellationRequested) throw new TaskCanceledException();

            using (var memoryStream = new MemoryStream())
            using (var stream = await resp.Content.ReadAsStreamAsync())
            {
                var buffer = new byte[8192];
                var bytesRead = 0; ;

                long? contentLength = resp.Content.Headers.ContentLength;
                var totalRead = 0;

                // send report
                progress?.Report(0);

                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    if (token.IsCancellationRequested) throw new TaskCanceledException();

                    if (contentLength != null)
                    {
                        progress?.Report(totalRead / (double)contentLength);
                    }

                    await memoryStream.WriteAsync(buffer, 0, bytesRead);
                    totalRead += bytesRead;
                }

                progress?.Report(1);
                byte[] bytes = memoryStream.ToArray();

                return new WebResponse(resp, bytes);
            }
        }
    }
}
