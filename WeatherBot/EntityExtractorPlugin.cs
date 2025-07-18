using System.ComponentModel;
using Azure;
using Azure.AI.TextAnalytics;
using Microsoft.SemanticKernel;

namespace WeatherBot;

public class EntityExtractorPlugin
{
    private readonly TextAnalyticsClient _client;
    
    public EntityExtractorPlugin(string languageKey, string languageEndpoint)
    {
        var credentials = new AzureKeyCredential(languageKey);
        var endpoint = new Uri(languageEndpoint);
        _client = new TextAnalyticsClient(endpoint, credentials);
    }
    
    [KernelFunction("get_entities")]
    [Description("Extract entities from the input text, such as locations or names.")]
    public List<(string text, string category, string subCategory, string confidenceScore)> GetEntities(
        [Description("The input text to analyze for entities.")] string userInput)
    {
        var identifiedEntities = new List<(string text, string category, string subCategory, string confidenceScore)>();
        
        var response = _client.RecognizeEntities(userInput);
        foreach (var entity in response.Value)
        {
            identifiedEntities.Add(
                (entity.Text, entity.Category.ToString(), entity.SubCategory, entity.ConfidenceScore.ToString("F2"))
            );
        }

        return identifiedEntities;
    }
}