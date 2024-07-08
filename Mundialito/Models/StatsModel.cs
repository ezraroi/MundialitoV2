
using System.Text.Json.Serialization;

namespace Mundialito.Models;

public class StatsModel {

    [JsonPropertyName("PointsPerMatch")]
    public PerGameModel PointsPerMatch { get; set; }

    [JsonPropertyName("CornersPointsPerMatch")]
    public PerGameModel CornersPointsPerMatch { get; set; }

    [JsonPropertyName("CardsPointsPerMatch")]
    public PerGameModel CardsPointsPerMatch { get; set; }

    [JsonPropertyName("MarkProbability")]
    public PerGameModel MarkProbability { get; set; }
    
    [JsonPropertyName("ResultProbability")]
    public PerGameModel ResultProbability { get; set; }
    
    [JsonPropertyName("NumOfBingos")]
    public PerGameModel NumOfBingos { get; set; }
}

public class PerGameModel {
    
    [JsonPropertyName("Category")]
    public string Category { get; set; }
    
    [JsonPropertyName("Overall")]
    public float Overall { get; set; }
    
    [JsonPropertyName("You")]
    public float You { get; set; }

    [JsonPropertyName("Leader")]
    public float Leader { get; set; }

    [JsonPropertyName("User")]
    public float User { get; set; }

    [JsonPropertyName("Followees")]
    public float Followees { get; set; }

    [JsonPropertyName("BestResult")]
    public BestResult BestResult { get; set; }
}

public class BestResult {
    
    [JsonPropertyName("Name")]
    public string Name { get; set;}
    
    [JsonPropertyName("Value")]
    public float Value { get; set; }
}