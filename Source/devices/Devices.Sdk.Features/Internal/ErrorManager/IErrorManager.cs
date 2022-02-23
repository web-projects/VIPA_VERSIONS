using System.Threading.Tasks;

namespace Devices.Sdk.Features.Internal.ErrorManager
{
    public interface IErrorManager
    {
        ValueTask <bool> ErrorSendingMessage(object message);
    }
}
