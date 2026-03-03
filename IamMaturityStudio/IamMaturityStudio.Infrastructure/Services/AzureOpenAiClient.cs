namespace IamMaturityStudio.Infrastructure.Services;

public interface IAzureOpenAiClient
{
    string Endpoint { get; }
}

public class AzureOpenAiClient : IAzureOpenAiClient
{
    public AzureOpenAiClient(string endpoint)
    {
        Endpoint = endpoint;
    }

    public string Endpoint { get; }
}