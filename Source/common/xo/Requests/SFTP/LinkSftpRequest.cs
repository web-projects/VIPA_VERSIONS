namespace XO.Requests.SFTP
{
    public class LinkSftpRequest
    {
        public string Hostname { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string DeviceHealthStatusFilename { get; set; }
        public int Port { get; set; }
    }
}
