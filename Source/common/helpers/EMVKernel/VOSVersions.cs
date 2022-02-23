namespace App.Helpers.EMVKernel
{
    public sealed class VOSVersions
    {
        public string ADKVault { get; set; }
        public string ADKAppManager { get; set; }
        public string ADKOpenProtocol { get; set; }
        public string ADKSRED { get; set; }

        public override string ToString()
        {
            return $"{ADKVault}|{ADKAppManager}|{ADKOpenProtocol}|{ADKSRED}";
        }
    }
}
