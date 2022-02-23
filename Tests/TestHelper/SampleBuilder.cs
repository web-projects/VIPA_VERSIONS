using System;
using System.Collections.Generic;
using System.Linq;

namespace TestHelper
{
    public static class SampleBuilder
    {/*
        public static LinkRequest LinkRequestSale() =>
            new LinkRequest()
            {
                TCCustID = RandomGenerator.GetRandomValue(6),
                TCPassword = RandomGenerator.BuildRandomString(7),
                Actions = new List<LinkActionRequest> { BuildLinkSaleAction() },
                MessageID = RandomGenerator.BuildRandomString(8),
            };

        public static LinkRequest LinkRequestDALStatus() =>
            new LinkRequest()
            {
                TCCustID = RandomGenerator.GetRandomValue(6),
                TCPassword = RandomGenerator.BuildRandomString(7),
                Actions = new List<LinkActionRequest> { BuildLinkDALGetStatus() },
                MessageID = RandomGenerator.BuildRandomString(8)
            };

        public static LinkRequest LinkRequestSession(LinkSessionActionType? linkSessionActionType = LinkSessionActionType.Initialize) =>
            new LinkRequest()
            {
                TCCustID = RandomGenerator.GetRandomValue(6),
                TCPassword = RandomGenerator.BuildRandomString(7),
                Actions = new List<LinkActionRequest> { BuildLinkSessionAction(linkSessionActionType: linkSessionActionType) },
                MessageID = RandomGenerator.BuildRandomString(8),
            };

        public static LinkActionRequest BuildLinkSaleAction(bool buildLocal = false)
        {
            var action = new LinkActionRequest
            {
                MessageID = RandomGenerator.BuildRandomString(RandomGenerator.rnd().Next(5, 16)),
                Action = LinkAction.Payment,
                DALRequest = buildLocal ? null : PopulateMockDALIdentifier(null, false),
                PaymentRequest = new LinkPaymentRequest
                {
                    RequestedAmount = RandomGenerator.GetRandomValue(4),
                    CurrencyCode = "USD",
                    PaymentType = LinkPaymentRequestType.Sale,
                    WorkflowControls = new LinkWorkflowControls() { CardEnabled = true },
                    PartnerRegistryKeys = new List<string>() { RandomGenerator.BuildRandomString(7) },
                    RequestedTenderType = LinkPaymentRequestedTenderType.Card,
                    CardWorkflowControls = new LinkCardWorkflowControls()
                }
            };
            return action;
        }

        public static LinkActionRequest BuildLinkDALGetStatus() =>
            new LinkActionRequest()
            {
                MessageID = RandomGenerator.BuildRandomString(RandomGenerator.rnd().Next(5, 16)),
                Action = LinkAction.DALAction,
                DALRequest = PopulateMockDALIdentifier(null, false),
                DALActionRequest = new LinkDALActionRequest()
                {
                    DALAction = LinkDALActionType.GetStatus
                }
            };


        public static LinkRequestIPA5Object LinkObjectsForEvents() =>
            new LinkRequestIPA5Object() 
            { 
                LinkActionResponseList = new List<LinkActionResponse>()
                {
                    new LinkActionResponse()
                    { 
                        MessageID = RandomGenerator.BuildRandomString(5),
                        DALResponse = new LinkDALResponse(),
                        PaymentResponse = new LinkPaymentResponse()
                        {
                            TCLinkResponse = new List<LinkNameValueResponse>()
                        }
                    }
                }
            };


        public static LinkRequestIPA5Object LinkRequestWithDeviceResponse(string manufacturer) =>
            new LinkRequestIPA5Object()
            {
                LinkActionResponseList = new List<LinkActionResponse>()
                {
                    new LinkActionResponse()
                    {
                        DALResponse = new LinkDALResponse()
                        {
                            Devices = new List<LinkDeviceResponse>()
                            {
                                new LinkDeviceResponse()
                                {
                                    Manufacturer = manufacturer
                                }
                            }
                        }
                    }
                }
            };

        public static LinkRequest SetDALStatusOnline(LinkRequest request)
        {
            var linkActionRequest = request.Actions.First();

            request.LinkObjects = SampleBuilder.LinkRequestWithDeviceResponse("MOCK");

            if (string.IsNullOrWhiteSpace(linkActionRequest.RequestID))
            {
                linkActionRequest.RequestID = Guid.NewGuid().ToString();
            }

            var linkActionResponse = new LinkActionResponse
            {
                MessageID = linkActionRequest.MessageID,
                RequestID = linkActionRequest.RequestID,
                DALResponse = new LinkDALResponse
                {
                    //OnlineStatus = true,
                    DALIdentifier = MockDALIdentifier(null)
                }
            };

            if (request.LinkObjects == null)
            {
                request.LinkObjects = new LinkRequestIPA5Object()
                {
                    RequestID = new Guid(linkActionRequest.RequestID),
                    LinkActionResponseList = new List<LinkActionResponse>(),
                    IdleRequest = false
                };
            }

            linkActionRequest.LinkObjects = new LinkActionRequestIPA5Object
            {
                RequestID = request.LinkObjects.RequestID,
                ActionResponse = linkActionResponse
            };

            return request;
        }

        public static LinkNameValueResponse TCLinkResponse(string name, string value) =>
            new LinkNameValueResponse()
            {
                Name = name,
                Value = value
            };

        private static LinkActionRequest BuildLinkSessionAction(bool buildLocal = false, LinkSessionActionType? linkSessionActionType = LinkSessionActionType.Initialize)
        {
            var action = new LinkActionRequest
            {
                MessageID = RandomGenerator.BuildRandomString(RandomGenerator.rnd().Next(5, 16)),
                Action = LinkAction.Session,
                DALRequest = buildLocal ? null : PopulateMockDALIdentifier(null, false),
                SessionRequest = new LinkSessionRequest
                {
                    SessionAction = linkSessionActionType,
                    IdleActions = new List<LinkActionRequest>
                    {
                        new LinkActionRequest
                        {
                            MessageID = RandomGenerator.BuildRandomString(RandomGenerator.rnd().Next(5, 16)),
                            Action = LinkAction.DALAction,
                            DALActionRequest = new LinkDALActionRequest
                            {
                                DALAction = LinkDALActionType.DeviceUI,
                                DeviceUIRequest = new LinkDeviceUIRequest
                                {
                                    UIAction = LinkDeviceUIActionType.KeyRequest,
                                    AutoConfirmKey = true,
                                    ReportCardPresented = true,
                                    MinLength = 1,
                                    MaxLength = 1
                                }
                            }
                        }
                    }
                }
            };
            return action;
        }

        public static LinkDALResponse MockDALResponse()
        {
            var dalResponse = new LinkDALResponse();
            dalResponse.DALIdentifier = MockDALIdentifier(null);
            return dalResponse;
        }

        private static LinkDALRequest PopulateMockDALIdentifier(LinkDALRequest linkDALRequest, bool addLookupPreference = true)
        {
            if (linkDALRequest == null)
            {
                linkDALRequest = new LinkDALRequest();
            }

            if (linkDALRequest.DALIdentifier == null)
            {
                linkDALRequest.DALIdentifier = MockDALIdentifier(linkDALRequest.DALIdentifier);
            }

            if (addLookupPreference && (linkDALRequest.DALIdentifier.LookupPreference == null))
            {
                linkDALRequest.DALIdentifier.LookupPreference = LinkDALLookupPreference.WorkstationName;
            }

            return linkDALRequest;
        }

        private static LinkDALIdentifier MockDALIdentifier(LinkDALIdentifier dalIdentifier)
        {
            if (dalIdentifier != null)
            {
                return dalIdentifier;
            }

            var rnd = new Random();
            dalIdentifier = new LinkDALIdentifier
            {
                DnsName = "Host" + rnd.Next(0, 999).ToString("000", null),
                IPv4 = rnd.Next(193, 254).ToString("000", null) + "." + rnd.Next(193, 254).ToString("000", null) + "." + rnd.Next(193, 254).ToString("000", null) + "." + rnd.Next(193, 254).ToString("000", null),
                Username = "User" + RandomGenerator.BuildRandomString(6)
            };

            return dalIdentifier;
        }
    */}
}
