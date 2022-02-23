using System;

namespace Config.AppConfig
{
    [Serializable]
    public class AppConfiguration
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Arguments { get; set; }
        public string Delay { get; set; }
        public override string ToString()
        {
            return $"{Name} ({Arguments})";
        }
    }
}
