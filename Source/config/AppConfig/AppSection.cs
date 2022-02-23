using System;
using System.Collections.Generic;

namespace Config.AppConfig
{
    [Serializable]
    public sealed class AppSection
    {
        public List<AppConfiguration> Apps { get; } = new List<AppConfiguration>();
        public int DefaultLaunchDelay { get; set; }
        public string Daemon { get; set; }
    }
}
