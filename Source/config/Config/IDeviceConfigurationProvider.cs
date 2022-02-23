using Devices.Common.AppConfig;
using Microsoft.Extensions.Configuration;

namespace Common.Config
{
    public interface IDeviceConfigurationProvider
    {
        void InitializeConfiguration();
        IConfiguration GetConfiguration();
        DeviceSection GetAppConfig();
        DeviceSection GetAppConfig(IConfiguration configuration);
    }
}
