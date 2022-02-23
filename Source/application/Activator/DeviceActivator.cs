using Ninject;
using DEVICE_CORE;
using DEVICE_CORE.Modules;
using DEVICE_CORE.Providers;
using System;
using static Common.Execution.Modes;

namespace DEVICE_CORE
{
    public class DeviceActivator
    {
        [Inject]
        internal IDeviceApplicationProvider DeviceApplicationProvider { get; set; }

        public DeviceActivator()
        {
            using (IKernel kernel = new DeviceKernelResolver().ResolveKernel())
            {
                kernel.Inject(this);
            }
        }

        public IDeviceApplication Start(string pluginPath)
        {
            if (string.IsNullOrWhiteSpace(pluginPath))
            {
                throw new ArgumentNullException(nameof(pluginPath));
            }

            IDeviceApplication application = DeviceApplicationProvider.GetDeviceApplication();
            application.Initialize(pluginPath);
            return application;
        }
    }
}
