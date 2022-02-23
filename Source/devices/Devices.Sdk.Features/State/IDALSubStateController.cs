using Devices.Common.AppConfig;
using Devices.Common.Helpers;
using Devices.Common.Interfaces;
using Devices.Sdk.Features.Cancellation;
using Devices.Sdk.Features.Interrupt;
using Devices.Sdk.Features.State.Actions;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Devices.Sdk.Features.State
{
    public interface IDALSubStateController : IStateControlTrigger<IDALSubStateAction>, IFeatureManagementTransient
    {
        DeviceSection Configuration { get; }
        //ILoggingServiceClient LoggingClient { get; }
        //IBrokerConnector Connector { get; }
        IPaymentDevice TargetDevice { get; }
        List<IPaymentDevice> TargetDevices { get; }
        IInterruptManager InterruptManager { get; }
        bool DidTimeoutOccur { get; }
        DeviceEvent DeviceEvent { get; }
        IDeviceHighLevelRegister Register { get; }
        bool DidCancellationOccur { get; }
        ConcurrentDictionary<string, string[]> AvailableFeatures { get; }
        void CancelCurrentSubStateAction(DeviceEvent deviceEvent);
        //CommunicationObject LastGetStatus { get; set; }
        void SaveState(object stateObject);
        IDeviceCancellationBroker GetDeviceCancellationBroker();
        bool RequestWorkflowCancellation();
        IPaymentDevice[] GetAvailableDevices();
    }
}
