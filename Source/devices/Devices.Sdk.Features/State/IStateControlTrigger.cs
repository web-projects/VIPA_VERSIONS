using System.Threading.Tasks;

namespace Devices.Sdk.Features.State
{
    public interface IStateControlTrigger<TStateAction>
    {
        Task Complete(TStateAction state);
        Task Error(TStateAction state);
    }
}
