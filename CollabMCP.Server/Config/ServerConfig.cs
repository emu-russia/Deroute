namespace CollabMCP.Server.Config;

public class ServerConfig
{
    public string Url { get; set; } = "http://0.0.0.0";
    public int Port { get; set; } = 5000;
    public string AdminApiKey { get; set; } = string.Empty;
    public string XmlStoragePath { get; set; } = "./sessions";
    public string LogPath { get; set; } = "./Logs";
    public int ThrottleIntervalMs { get; set; } = 33;
}
