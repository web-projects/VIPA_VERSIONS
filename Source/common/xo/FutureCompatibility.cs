using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace XO
{
    //Required base class for all IPALink5 classes to provide compatibility with future contracts
    //Note: does not prevent parsing errors (exceptions) when incorrect types are used for defined values
    public partial class LinkFutureCompatibility
    {
        [JsonExtensionData]
        public IDictionary<string, JToken> Properties { get; set; }
    }
}
