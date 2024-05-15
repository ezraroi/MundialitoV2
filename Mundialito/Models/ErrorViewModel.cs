using System.Text.Json.Serialization;

namespace Mundialito.Models;

public class ErrorViewModel
{
    [JsonPropertyName("RequestId")]
    public string? RequestId { get; set; }

    [JsonPropertyName("ShowRequestId")]
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}

public class ErrorMessage {
    [JsonPropertyName("Message")]
    public string? Message { get; set; }
}
