using System.IO.Compression;
using Microsoft.AspNetCore.Mvc;

namespace Mundialito.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GenericProxyController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        public GenericProxyController(IHttpClientFactory _httpClientFactory)
        {
            _httpClient = _httpClientFactory.CreateClient("MyHttpClient"); ;
        }

        [HttpGet("{*url}")]
        [HttpPost("{*url}")]
        [HttpPut("{*url}")]
        [HttpDelete("{*url}")]
        [HttpPatch("{*url}")]
        [HttpOptions("{*url}")]
        [HttpHead("{*url}")]
        public async Task<IActionResult> Proxy(
            string url,
            [FromQuery] IDictionary<string, string> queryParams)
        {
            // Construct the full URL
            var requestUrl = new Uri(url + QueryString.Create(queryParams).Value);

            var requestMessage = new HttpRequestMessage
            {
                Method = new HttpMethod(Request.Method),
                RequestUri = requestUrl
            };

            // Copy headers from the incoming request to the outgoing request
            foreach (var header in Request.Headers)
            {
                if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
                {
                    requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }

            // Copy body if applicable
            if (Request.ContentLength > 0)
            {
                requestMessage.Content = new StreamContent(Request.Body);
                requestMessage.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(Request.ContentType);
            }

            var response = await _httpClient.SendAsync(requestMessage);
            Stream decompressedStream;
            if (response.Content.Headers.ContentEncoding.Contains("gzip"))
            {
                decompressedStream = new GZipStream(await response.Content.ReadAsStreamAsync(), CompressionMode.Decompress);
            }
            else
            {
                decompressedStream = await response.Content.ReadAsStreamAsync();
            }
            var content = new StreamReader(decompressedStream).ReadToEnd();
            return new ContentResult
            {
                StatusCode = (int)response.StatusCode,
                Content = content,
                ContentType = response.Content.Headers.ContentType?.ToString()
            };
        }
    }
}
