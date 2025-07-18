using System.ComponentModel;
using Azure;
using Azure.AI.TextAnalytics;
using Microsoft.SemanticKernel;

namespace WeatherBot;

public class WeatherPlugin
{
    private readonly TextAnalyticsClient _client;
    
    public WeatherPlugin(string mapsApiKey, string location)
    {
        var credentials = new AzureKeyCredential(mapsApiKey);
        var endpoint = new Uri(@"https://atlas.microsoft.com/weather/currentConditions/json?api-version=1.1
                                &query={location}8&subscription-key={mapsApiKey}");
        _client = new TextAnalyticsClient(endpoint, credentials);
    }
    
    [KernelFunction("get_weather")]
    [Description("Gets today's weather for a specific city or location.")]
    public string GetWeather(string city)
    {
        // This is a placeholder implementation. In a real application, you would call a weather API.
        string returnDetails = $"The weather in {city} today is sunny with a high of 25°C and a low of 15°C.";
        return returnDetails;
    }
}