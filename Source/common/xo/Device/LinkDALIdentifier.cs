using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Common.XO.Device
{
    public class LinkDALIdentifier
    {
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum LinkDALLookupPreference
    {
        WorkstationName,
        DnsName,
        IPv4,
        IPv6,
        Username
    }
}
