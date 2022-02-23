using Ninject;
using Ninject.Modules;
using System;
using System.Collections.Generic;
using System.Text;

namespace DEVICE_CORE.Modules
{
    public interface IDeviceKernelModuleResolver
    {
        IKernel ResolveKernel(params NinjectModule[] modules);
    }
}
