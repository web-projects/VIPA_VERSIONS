using Devices.Core.State.SubWorkflows;

namespace Devices.Core.State.Interfaces
{
    internal interface ISubWorkflowHook
    {
        void Hook(IDeviceSubStateController controller);
        void UnHook();
    }
}
