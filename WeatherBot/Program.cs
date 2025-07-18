// Import packages

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using WeatherBot;

// Initial setup for the application host
IHost host = Host.CreateApplicationBuilder(args).Build();

// Retrieve the application's configuration service
IConfiguration config = host.Services.GetRequiredService<IConfiguration>();

// Bind the "ProjectSecrets" section from configuration to a strongly-typed object
ProjectSecrets secretsFromUserSecrets = config.GetSection("ProjectSecrets").Get<ProjectSecrets>()
                                        ?? throw new InvalidOperationException("ProjectSecrets section " +
                                                                               "is missing or not properly configured.");

// Bind the "ProjectSettings" section from configuration to a strongly-typed object, or throw if missing
ProjectSettings projectSettings = config.GetSection("ProjectSettings").Get<ProjectSettings>()
                                  ?? throw new InvalidOperationException("ProjectSettings section " +
                                                                         "is missing or not properly configured.");

// Extract individual values from the loaded secrets and settings
string endpoint = secretsFromUserSecrets.Endpoint;
string azureOpenAiApiKey = secretsFromUserSecrets.AzureOpenAiApiKey;
string chatDeploymentName = projectSettings.ChatDeploymentName;
string apiVersion = projectSettings.ApiVersion;
string languageKey = secretsFromUserSecrets.LanguageKey;
string languageEndpoint = secretsFromUserSecrets.LanguageEndpoint;
string mapsApiKey = secretsFromUserSecrets.AzureMapsApiKey;
string defaultLocation = projectSettings.DefaultLocation;

// Alternative way to get a single value from configuration (commented out)
// string? chatDeploymentName = config.GetValue<string>("ProjectSettings:ChatDeploymentName");

var kernelBuilder = Kernel.CreateBuilder();

// Configure the kernel builder
kernelBuilder
    .AddAzureOpenAIChatCompletion(
        deploymentName: chatDeploymentName,
        endpoint: endpoint,
        apiKey: azureOpenAiApiKey,
        apiVersion: apiVersion
    );
    //.Services.AddLogging(services => services.AddConsole().SetMinimumLevel(LogLevel.Trace));


// Add logging to the kernel builder for debugging and tracing
//kernelBuilder.Services.AddLogging(services => services.AddConsole().SetMinimumLevel(LogLevel.Trace));
var kernel = kernelBuilder.Build();
var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

// Define the system message for the AI assistant
Console.WriteLine("Please define the personality and behavior of the AI assistant.");
string? systemMessage = Console.ReadLine();
if (string.IsNullOrEmpty(systemMessage))
{
    // If no system message is provided, use a default one
    Console.WriteLine("No system message provided. Using default system message.");
    systemMessage =
        @"You are a helpful and funny assistant that provides weather information and 
    also extract entities from the user input. You always greet the user at the start of the 
    conversation and say goodbye at the end of the conversation.";
    Console.WriteLine(systemMessage);
}
systemMessage = systemMessage.Trim();

// Add the WeatherPlugin with the required parameters
var weatherPlugin = new WeatherPlugin(mapsApiKey, defaultLocation);
//kernel.Plugins.AddFromType<WeatherPlugin>("get_weather");
kernel.Plugins.AddFromObject(weatherPlugin, "get_weather");
// Add the EntityExtractorPlugin with the required parameters
var entityExtractorPlugin = new EntityExtractorPlugin(languageKey, languageEndpoint);
kernel.Plugins.AddFromObject(entityExtractorPlugin, "get_entities");

// Enable planning
AzureOpenAIPromptExecutionSettings azureOpenAiPromptExecutionSettings = new()
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
    ChatSystemPrompt = systemMessage,
    MaxTokens = 1000,
    Temperature = 0.7f
};

// Open a conversation with a greeting
var openingMessage = @"Hello! I am an useful assistant. Please ask me question about any topic.";
Console.WriteLine($"Assistant > {openingMessage}");

// Create a history store the conversation
var history = new ChatHistory();
// Manage the chat history
var chatHistoryReducer = new ChatHistorySummarizationReducer(chatCompletionService, 4);
// Add the opening message to the chat history
history.AddAssistantMessage(openingMessage);

string? userInput;

do
{
    // Chat with the user
    Console.Write("User > ");
    userInput = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(userInput))
    {
        Console.WriteLine("Valid input is required. Please try again.");
        return;
    }
    else
    {
        // Add user input to the chat history
        history.AddUserMessage(userInput);

        // Get the response from the AI
        var result = await chatCompletionService.GetChatMessageContentsAsync
        (chatHistory: history,
            executionSettings: azureOpenAiPromptExecutionSettings,
            kernel: kernel);

        history.AddMessage(result[0].Role, result[0].Content ?? string.Empty);

        // Reduce the chat history to keep it manageable
        var optimizedChatHistory = await chatHistoryReducer.ReduceAsync(history);

        // Update the history with the reduced chat history
        if (optimizedChatHistory != null)
        {
            history = new ChatHistory(optimizedChatHistory);
        }

        // Print the results
        Console.WriteLine("Assistant > " + result[0].Content);
    }
} while (userInput.ToLower() != "quit");