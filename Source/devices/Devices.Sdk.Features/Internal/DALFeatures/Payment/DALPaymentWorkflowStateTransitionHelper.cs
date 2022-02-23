using Devices.Core.State;
using Devices.Sdk.Features.State;
using static Devices.Sdk.Features.State.DALSubWorkflowState;

namespace Devices.Sdk.Features.Internal.DALFeatures.Payment
{
    public class DALPaymentWorkflowStateTransitionHelper : IDeviceWorkflowFeatureStateTransitionHelper
    {
        private static DALSubWorkflowState ComputeGetStatusStateTransition(bool exception) =>
         exception switch
         {
             true => SanityCheck,
             false => SanityCheck
         };

        private static DALSubWorkflowState ComputePresentCardStateTransition(bool exception) =>
            exception switch
            {
                true => SanityCheck,
                false => SanityCheck
            };
        private static DALSubWorkflowState ComputeSignatureStateTransition(bool exception) =>
            exception switch
            {
                true => SanityCheck,
                false => SanityCheck
            };

        private static DALSubWorkflowState ComputeGetCardDataStateTransition(bool exception) =>
            exception switch
            {
                true => SanityCheck,
                false => SanityCheck
            };


        private static DALSubWorkflowState ComputeGetManualPANDataStateTransition(bool exception) =>
            exception switch
            {
                true => SanityCheck,
                false => SanityCheck
            };

        private static DALSubWorkflowState ComputeGetVerifyAmountDataStateTransition(bool exception) =>
            exception switch
            {
                true => SanityCheck,
                false => SanityCheck
            };

        private static DALSubWorkflowState ComputeGetCreditOrDebitStateTransition(bool exception) =>
            exception switch
            {
                true => SanityCheck,
                false => SanityCheck
            };

        private static DALSubWorkflowState ComputeGetZipStateTransition(bool exception) =>
            exception switch
            {
                true => SanityCheck,
                false => SanityCheck
            };

        private static DALSubWorkflowState ComputeGetPinStateTransition(bool exception) =>
            exception switch
            {
                true => SanityCheck,
                false => SanityCheck
            };

        private static DALSubWorkflowState ComputeRemoveCardStateTransition(bool exception) =>
            exception switch
            {
                true => SanityCheck,
                false => SanityCheck
            };

        private static DALSubWorkflowState ComputeDeviceUIStateTransition(bool exception) =>
            exception switch
            {
                true => SanityCheck,
                false => SanityCheck
            };

        private static DALSubWorkflowState ComputeSanityCheckStateTransition(bool exception) =>
            exception switch
            {
                true => RequestComplete,
                false => RequestComplete
            };

        private static DALSubWorkflowState ComputeRequestCompletedStateTransition(bool exception) =>
            exception switch
            {
                true => Undefined,
                false => Undefined
            };

        public DALSubWorkflowState GetNextState(DALSubWorkflowState state, bool exception) =>
            state switch
            {
                GetStatus => ComputeGetStatusStateTransition(exception),
                PresentCard => ComputePresentCardStateTransition(exception),
                GetCardData => ComputeGetCardDataStateTransition(exception),
                GetManualPANData => ComputeGetManualPANDataStateTransition(exception),
                GetVerifyAmount => ComputeGetVerifyAmountDataStateTransition(exception),
                GetCreditOrDebit => ComputeGetCreditOrDebitStateTransition(exception),
                GetZip => ComputeGetZipStateTransition(exception),
                GetPin => ComputeGetPinStateTransition(exception),
                RemoveCard => ComputeRemoveCardStateTransition(exception),
                DeviceUI => ComputeDeviceUIStateTransition(exception),
                SanityCheck => ComputeSanityCheckStateTransition(exception),
                RequestComplete => ComputeRequestCompletedStateTransition(exception),
                _ => throw new StateException($"Invalid state transition '{state}' requested.")
            };
    }
}
