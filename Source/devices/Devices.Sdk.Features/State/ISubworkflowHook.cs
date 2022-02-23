namespace Devices.Sdk.Features.State
{
    public interface ISubWorkflowHook
    {
        void Hook(IDALSubStateController controller);
        void UnHook();
    }
}
