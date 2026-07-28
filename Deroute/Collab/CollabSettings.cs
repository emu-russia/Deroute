using System;
using System.Globalization;

namespace DerouteSharp.Collab
{
    public class CollabSettings
    {
        public bool Enabled { get; set; }
        public string ServerUrl { get; set; } = "http://localhost:5000";
        public string ApiKey { get; set; } = string.Empty;
        public string UserId { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public string Username { get; set; }
        public int ReconnectDelayMs { get; set; } = 2000;
        public int MaxReconnectAttempts { get; set; } = 50;

        public CollabSettings()
        {
            UserId = DateTime.Now.Ticks.ToString("x").Substring(0, 8);
            Username = System.Environment.UserName;
        }
    }
}
