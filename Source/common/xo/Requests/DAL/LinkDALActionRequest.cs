using Newtonsoft.Json.Converters;
using System.Text.Json.Serialization;
using XO;

namespace Common.XO.Requests.DAL
{
    public partial class LinkDALActionRequest : LinkFutureCompatibility
    {
        public XO.Responses.LinkErrorValue Validate(bool synchronousRequest = true)
        {
            // No validation on these values
            return null;
        }

        public LinkDALActionType? DALAction { get; set; }
    }

    //DAL action selection
    [JsonConverter(typeof(StringEnumConverter))]
    public enum LinkDALActionType
    {
        ReportVipaVersions,
        SetDeviceIdle
    }
}
