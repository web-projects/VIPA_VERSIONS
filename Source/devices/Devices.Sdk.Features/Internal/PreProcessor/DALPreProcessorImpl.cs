using IPA5.Core.Constants;
using Devices.Common;
using Devices.Common.Helpers;
using Devices.Sdk.Features.Cancellation;
using Devices.Sdk.Features.Internal.State;
using Devices.Sdk.Features.State;
using Devices.Sdk.Features.State.Providers;
using Common.XO.ProtoBuf;
using Common.XO.Requests.DAL;
using Common.XO.Responses.DAL;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using LinkActionResponse = XO.Responses.LinkActionResponse;
using LinkRequest = XO.Requests.LinkRequest;

namespace Devices.Sdk.Features.Internal.PreProcessor
{
    internal sealed class DALPreProcessorImpl : IDALPreProcessor
    {
        internal delegate void PreprocessorFunction(CommunicationHeader header, LinkRequest request, IDALSubStateController action);

        private IDALStateController context;

        public DALPreProcessorImpl(IDALStateController controller) => context = controller;

        public bool CanHandleRequest(LinkRequest request, IDALSubStateController subStateController)
        {
            try
            {
                PreprocessorFunction preprocessorFunction = FindRequestFunction(request, subStateController);
                return preprocessorFunction != null;
            }
            catch (Exception ex)
            {
                _ = context.LoggingClient.LogErrorAsync(ex.Message, ex);
            }
            return false;
        }

        public void HandleRequest(CommunicationHeader header, LinkRequest request, IDALSubStateController subStateController)
        {
            PreprocessorFunction handlerFunction = FindRequestFunction(request, subStateController);
            if (handlerFunction != null)
            {
                handlerFunction(header, request, subStateController);
            }
            else
            {
                _ = context.LoggingClient.LogErrorAsync("Unable to find SubStateControllerHandlerFunction");
            }
        }

        internal PreprocessorFunction FindRequestFunction(LinkRequest request, IDALSubStateController controller)
        {
            if (controller is null)
            {
                return null;
            }

            LinkDALActionType? dalAction = request?.Actions?.FirstOrDefault()?.DALActionRequest?.DALAction;
            if (dalAction == LinkDALActionType.CancelPayment)
            {
                string dalActionResponseValue = request.LinkObjects?.LinkActionResponseList?.FirstOrDefault()?.DALActionResponse?.Value;
                if (string.Equals(dalActionResponseValue, DalValueCodes.TimeoutPaymentCancel, StringComparison.OrdinalIgnoreCase))
                {
                    return TimeOutPayment;
                }

                if (string.Equals(dalActionResponseValue, DalValueCodes.NetworkConnectionFailure, StringComparison.OrdinalIgnoreCase))
                {
                    return CancelNetworkFailure;
                }

                return CancelPayment;
            }

            if (dalAction == LinkDALActionType.EndPreSwipeMode)
            {
                return EndCardPreSwipeMode;
            }

            if (dalAction == LinkDALActionType.GetPayment)
            {
                return DalBusy;
            }

            return null;
        }

        public void CancelNetworkFailure(CommunicationHeader header, LinkRequest request, IDALSubStateController subStateController)
        {
            if (subStateController is null)
            {
                return;
            }

            try
            {
                subStateController.CancelCurrentSubStateAction(DeviceEvent.NetworkConnectionFailure);
            }
            catch (Exception ex)
            {
                _ = subStateController.LoggingClient.LogErrorAsync("Exception occurred attempting to cancel payment from network failure.", ex);
            }
        }

        public void CancelPayment(CommunicationHeader header, LinkRequest request, IDALSubStateController subStateController)
        {
            if (subStateController is null)
            {
                return;
            }

            try
            {
                //Cancel current action in SubStateController
                subStateController.CancelCurrentSubStateAction(DeviceEvent.CancellationRequest);

                //Respond to Servicer that Cancel Payment was issued.
                PopulateCancelResponse(request);

                if (IsTestPedRequest(request))
                {
                    _ = subStateController.LoggingClient.LogInfoAsync("TestPED request cancelled by operator; do not forward onto Servicer.");
                }
                else if (subStateController.Connector is { })
                {
                    _ = subStateController.Connector.PublishAsync(JsonConvert.SerializeObject(request), header, ServiceType.Servicer);
                }
                else
                {
                    _ = subStateController.LoggingClient.LogErrorAsync("DALPreProcessor Publishing Error: subStateController has no Connector!");
                }
            }
            catch (Exception ex)
            {
                _ = subStateController.LoggingClient.LogErrorAsync("Exception occurred attempting cancel payment.", ex);
            }
        }

        public void TimeOutPayment(CommunicationHeader header, LinkRequest request, IDALSubStateController subStateController)
        {
            if (subStateController is null)
            {
                return;
            }

            try
            {
                //Cancel current action in SubStateController
                subStateController.CancelCurrentSubStateAction(DeviceEvent.RequestTimeout);
            }
            catch (Exception ex)
            {
                _ = subStateController.LoggingClient.LogErrorAsync("Exception occurred attempting to time out payment.", ex);
            }
        }

        public void EndCardPreSwipeMode(CommunicationHeader header, LinkRequest request, IDALSubStateController subStateController)
        {
            if (subStateController is null)
            {
                return;
            }

            try
            {
                //Cancel current action in SubStateController
                subStateController.CancelCurrentSubStateAction(DeviceEvent.CancellationRequest);

                IDeviceCancellationBroker cancellationBroker = subStateController.GetDeviceCancellationBroker();
                IPaymentDevice targetDevice = subStateController.TargetDevice;

                if (targetDevice is ICardDevice cardDevice)
                {
                    _ = cancellationBroker.ExecuteWithTimeoutAsync<LinkRequest>(_ => cardDevice.EndPreSwipeMode(request, _), request.GetAppropriateTimeoutSeconds(Timeouts.DALCardCaptureTimeout), CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                _ = subStateController.LoggingClient.LogErrorAsync($"DALPreProcessor Error: {ex.Message}", ex);
            }
        }

        public void DalBusy(CommunicationHeader header, LinkRequest request, IDALSubStateController subStateController)
        {
            if (subStateController is null)
            {
                return;
            }

            try
            {
                //Respond to Servicer that Dal is busy
                if (request.LinkObjects.LinkActionResponseList == null)
                {
                    request.LinkObjects.LinkActionResponseList = new List<LinkActionResponse>() { new LinkActionResponse() };
                }
                if (request.LinkObjects.LinkActionResponseList.Count == 0)
                {
                    request.LinkObjects.LinkActionResponseList.Add(new LinkActionResponse());
                }
                if (request.LinkObjects.LinkActionResponseList[0].DALActionResponse == null)
                {
                    request.LinkObjects.LinkActionResponseList[0].DALActionResponse = new LinkDALActionResponse();
                }
                request.LinkObjects.LinkActionResponseList[0].DALActionResponse.Status = DalStatusCodes.DalBusy;

                if (subStateController.Connector is { })
                {
                    _ = subStateController.Connector.PublishAsync(JsonConvert.SerializeObject(request), header, ServiceType.Servicer);
                }
                else
                {
                    _ = subStateController.LoggingClient.LogErrorAsync($"DALPreProcessor Publishing Error: subStateController has no Connector!");
                }
            }
            catch (Exception ex)
            {
                _ = subStateController.LoggingClient.LogErrorAsync($"DALPreProcessor Error: {ex.Message}", ex);
            }
        }

        private static bool IsTestPedRequest(LinkRequest request)
            => request.TCCustID == MonitorConstants.TestPedCustId
                && string.Equals(request.TCPassword, MonitorConstants.TestPedPassword, StringComparison.Ordinal)
                && (request.Actions?.FirstOrDefault()?.PaymentRequest?.Demo ?? false);

        private void PopulateCancelResponse(LinkRequest request)
        {
            request.LinkObjects.LinkActionResponseList ??= new List<LinkActionResponse>();

            if (request.LinkObjects.LinkActionResponseList.Count == 0)
            {
                request.LinkObjects.LinkActionResponseList.Add(new LinkActionResponse());
            }

            request.LinkObjects.LinkActionResponseList[0].DALActionResponse ??= new LinkDALActionResponse();

            request.LinkObjects.LinkActionResponseList[0].DALActionResponse.Status = DalStatusCodes.CancelRequested;
        }
    }
}
