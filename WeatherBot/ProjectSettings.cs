namespace WeatherBot;

public class ProjectSettings
{
    public required string ChatDeploymentName { get; init; }
    public required string TextDeploymentName { get; init; }
    public required string ImgDeploymentName { get; init; }
    public required string EmbeddingDeploymentName { get; init; }
    public required string ApiVersion { get; init; }
    public required string DefaultLocation { get; init; }
}