using Devices.Common;
using System.Collections.Generic;
using System.Linq;
using Common.XO.Device;
using Common.XO.Private;
using Common.XO.Requests;
using Common.XO.Responses;

namespace Devices.Core.Helpers
{
    public static class ResponseBuilder
    {
        public static LinkDeviceRequestObject ErrorResponse(string typeStr, string codeStr, string descStr)
        {
            return new LinkDeviceRequestObject
            {
                DeviceResponseData = new LinkDeviceActionResponse
                {
                    Errors = new List<LinkErrorValue>
                    {
                        new LinkErrorValue
                        {
                            Code = codeStr,
                            Type = typeStr,
                            Description = descStr
                        }
                    }
                }
            };
        }

        public static void SubworkflowErrorResponse(DeviceInformation deviceInformation, LinkRequest linkRequest, string type, string code, string description)
        {
            if (linkRequest != null)
            {
                LinkActionRequest linkActionRequest = linkRequest?.Actions.First();
                if (linkActionRequest.DeviceRequest == null)
                {
                    linkActionRequest.DeviceRequest = new LinkDeviceRequest();
                }
                /*linkActionRequest.DeviceRequest.LinkObjects = ErrorResponse(type, code, description);

                if (linkRequest.LinkObjects.LinkActionResponseList == null)
                {
                    linkRequest.LinkObjects.LinkActionResponseList[0].DALActionResponse = new LinkDeviceActionResponse();
                }
                linkRequest.LinkObjects.LinkActionResponseList[0].DALResponse = new LinkDALResponse
                {
                    Devices = new List<LinkDeviceResponse>
                    {
                        new LinkDeviceResponse
                        {
                            Manufacturer = deviceInformation?.Manufacturer,
                            Model = deviceInformation?.Model,
                            SerialNumber = deviceInformation?.SerialNumber,
                            //CardWorkflowControls = linkActionRequest.PaymentRequest?.CardWorkflowControls
                        }
                    }
                };*/

            }
        }
    }
}
