using System.IO.Compression;
using Microsoft.AspNetCore.Mvc;

namespace Mundialito.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GenericProxyController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;

        public GenericProxyController(IHttpClientFactory _httpClientFactory, ILogger<GenericProxyController> logger)
        {
            _httpClient = _httpClientFactory.CreateClient("MyHttpClient"); ;
            _logger = logger;
        }

        [HttpGet("{*url}")]
        [HttpPost("{*url}")]
        [HttpPut("{*url}")]
        [HttpDelete("{*url}")]
        [HttpPatch("{*url}")]
        [HttpOptions("{*url}")]
        [HttpHead("{*url}")]
        public async Task<IActionResult> Proxy([FromQuery] string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return BadRequest("URL is required");
            }
            _logger.LogInformation($"Proxying request to {url}");
            try
            {
                // Construct the full URI
                var uri = new Uri(url);
                var requestMessage = new HttpRequestMessage
                {
                    Method = new HttpMethod(Request.Method),
                    RequestUri = uri
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
                _logger.LogInformation($"Got response from {url}");
                _logger.LogInformation($"Stauts: {response.StatusCode}");
                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode);
                }

                // Decompress the response content if it's compressed
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

                return Content(content, response.Content.Headers.ContentType?.ToString());
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
