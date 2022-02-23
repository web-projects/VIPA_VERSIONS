using Common.Config;
using DEVICE_CORE.Providers;
using Ninject.Modules;
using Devices.Core.Providers;
using Devices.Core.SerialPort;
using Devices.Core.SerialPort.Interfaces;
using Devices.Core.State.Management;
using Devices.Core.State.Providers;

namespace DEVICE_CORE.Modules
{
    public class CoreModule : NinjectModule
    {
        public override void Load()
        {
            Bind<IDeviceApplicationProvider>().To<DeviceApplicationProvider>();
            Bind<IDeviceConfigurationProvider>().To<DeviceConfigurationProvider>();
            Bind<IDeviceStateActionControllerProvider>().To<DeviceStateActionControllerProvider>();
            Bind<IDeviceStateManager>().To<DeviceStateManagerImpl>();
            Bind<ISubStateManagerProvider>().To<SubStateManagerProviderImpl>();
            Bind<IControllerVisitorProvider>().To<ControllerVisitorProvider>();
            Bind<ISerialPortMonitor>().To<SerialPortMonitor>();
            Bind<IDeviceCancellationBrokerProvider>().To<DeviceCancellationBrokerProviderImpl>();
            Bind<DeviceActivator>().ToSelf();
            Bind<DeviceApplication>().ToSelf();
        }
    }
}
