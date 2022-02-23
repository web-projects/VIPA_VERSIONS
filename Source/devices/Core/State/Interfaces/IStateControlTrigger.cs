using System.Threading.Tasks;

namespace Devices.Core.State
{
    public interface IStateControlTrigger<TStateAction>
    {
        Task Complete(TStateAction state);
        Task Error(TStateAction state);
    }
}
