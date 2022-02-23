using Common.XO.Requests;

namespace Devices.Sdk.Features
{
    public interface IDeviceHighLevelRegister
    {
        LEBO LastAsyncBrokerOutcome { get; set; }
        LinkRequest LinkRequest { get; set; }
    }
}
