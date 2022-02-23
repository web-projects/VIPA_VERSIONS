using Common.XO.Requests;

namespace Devices.Sdk.Features
{
    public sealed class DeviceHighLevelRegisterImpl : IDeviceHighLevelRegister
    {
        LEBO IDeviceHighLevelRegister.LastAsyncBrokerOutcome { get; set; } = LEBO.Unknown;
        LinkRequest IDeviceHighLevelRegister.LinkRequest { get; set; }
    }
}
