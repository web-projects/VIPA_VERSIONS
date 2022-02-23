using Ninject;
using DEVICE_CORE.Modules;

namespace DEVICE_CORE.Providers
{
    internal class DeviceApplicationProvider : IDeviceApplicationProvider
    {
        public IDeviceApplication GetDeviceApplication()
        {
            DeviceApplication application = new DeviceApplication();

            using (IKernel kernel = new DeviceKernelResolver().ResolveKernel())
            {
                kernel.Inject(application);
            }

            return application;
        }
    }
}