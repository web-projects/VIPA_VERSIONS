using Common.XO.Requests;
using Execution;
using System.Threading.Tasks;

namespace DEVICE_CORE
{
    public interface IDeviceApplication
    {
        void Initialize(string pluginPath);
        Task Run(AppExecConfig appConfig);
        Task Command(LinkDeviceActionType action);
        void Shutdown();
    }
}
