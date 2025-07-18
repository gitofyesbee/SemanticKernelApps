namespace WeatherBot;

public class ProjectSecrets
{
    public required string AzureOpenAiApiKey { get; init; }
    public required string Endpoint { get; init; }
    public required string AzureMapsApiKey { get; init; }
    public required string LanguageKey { get; init; }
    public required string LanguageEndpoint { get; init; }
}