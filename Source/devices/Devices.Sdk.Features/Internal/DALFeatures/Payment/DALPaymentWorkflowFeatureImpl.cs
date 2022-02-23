using Common.XO.Requests;
using Common.XO.Requests.DAL;
using Devices.Common.Helpers;
using Devices.Common.Interfaces;
using Devices.Sdk.Features.State;
using Devices.Sdk.Features.State.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using static Devices.Common.SupportedDevices;
using static Devices.Common.SupportedFeatures;
using static Devices.Sdk.Features.DeviceFeatureType;
using static Devices.Sdk.Features.State.DALSubWorkflowState;

namespace Devices.Sdk.Features.Internal.DALFeatures.Payment
{
    internal class DALPaymentWorkflowFeatureImpl : IDeviceWorkflowFeature
    {
        readonly Lazy<IDeviceWorkflowFeatureStateTransitionHelper> transitionHelper =
            new Lazy<IDeviceWorkflowFeatureStateTransitionHelper>(
                () => new DALPaymentWorkflowStateTransitionHelper(), true
        );

        IDeviceFeatureManager IDeviceFeature.Context { get; set; }

        public string Name { get; } = PaymentWorkflowFeature;

        public DeviceFeatureType FeatureType { get; } = Workflow;

        public string[] SupportedDeviceTypes { get; } = new string[]
        {
            VerifoneDeviceType,
            Simulator_Device,
            NullDeviceType
        };

        public string[] SupportedModels { get; } = new string[]
        {
            Verifone_M400_Device,
            Verifone_M400BT_Device,
            Verifone_P400_Device,
            Verifone_P200_Device,
            Verifone_P200Plus_Device,
            Verifone_UX300_Device,
            Verifone_UX301_Device,
            Simulator_Device,
            Null_Device,
            MagTek_ExcellaImageSafe_Device
        };

        public string UserFriendlyName => "Payment";

        public void Dispose()
        {
            // Nothing to dispose of goes brrrrr...
        }

        public Dictionary<DALSubWorkflowState, Func<IDALSubStateController, IDALSubStateAction>> GetAvailableStateActions()
            => new Dictionary<DALSubWorkflowState, Func<IDALSubStateController, IDALSubStateAction>>
            {
                [GetStatus] = (IDALSubStateController _) => new DALGetStatusSubStateAction(_),
                [PresentCard] = (IDALSubStateController _) => new DALPresentCardSubStateAction(_),
                [GetCardData] = (IDALSubStateController _) => new DALGetCardDataSubStateAction(_),
                [GetManualPANData] = (IDALSubStateController _) => new DALGetManualPANDataSubStateAction(_),
                [GetVerifyAmount] = (IDALSubStateController _) => new DALGetVerifyAmountSubStateAction(_),
                [GetCreditOrDebit] = (IDALSubStateController _) => new DALGetCreditOrDebitSubStateAction(_),
                [GetZip] = (IDALSubStateController _) => new DALGetZipSubStateAction(_),
                [GetPin] = (IDALSubStateController _) => new DALGetPinSubStateAction(_),
                [RemoveCard] = (IDALSubStateController _) => new DALRemoveCardSubStateAction(_),
                [DeviceUI] = (IDALSubStateController _) => new DALDeviceUISubStateAction(_),
                [SanityCheck] = (IDALSubStateController _) => new DALSanityCheckSubStateAction(_),
                [RequestComplete] = (IDALSubStateController _) => new DALRequestCompleteSubStateAction(_)
            };

        public DALSubWorkflowState GetInitialStateAction(LinkRequest request)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            LinkActionRequest linkActionRequest = request.Actions.FirstOrDefault();
            DALSubWorkflowState proposedState = ((linkActionRequest.DALActionRequest?.DALAction ?? LinkDALActionType.GetPayment) switch
            {
                LinkDALActionType.GetStatus => GetStatus,
                LinkDALActionType.GetPayment => GetCardData,
                LinkDALActionType.StartManualPayment => GetManualPANData,
                LinkDALActionType.GetVerifyAmount => GetVerifyAmount,
                LinkDALActionType.GetCreditOrDebit => GetCreditOrDebit,
                LinkDALActionType.GetZIP => GetZip,
                LinkDALActionType.GetPIN => GetPin,
                LinkDALActionType.RemoveCard => RemoveCard,
                LinkDALActionType.DeviceUI => DeviceUI,
                LinkDALActionType.DeviceUITest => DeviceUI,
                LinkDALActionType.GetTestPayment => GetCardData,
                LinkDALActionType.GetTestStatus => GetStatus,
                _ => DALSubWorkflowState.Undefined
            });

            return proposedState;
        }

        public IDeviceWorkflowFeatureStateTransitionHelper GetTransitionHelper() => transitionHelper.Value;

        public bool Validate(LinkRequest request, IPaymentDevice[] cardDevices, out string errorReason)
        {
            if (request.Actions?[0]?.DALActionRequest?.DALAction != LinkDALActionType.GetStatus)
            {
                if (!WorkflowFeatureValidator.Validate(SupportedModels, request, cardDevices))
                {
                    errorReason = "Unable to find a supported device for Payment Feature";
                    return false;
                }
            }
            errorReason = null;
            return true;
        }
    }
}
