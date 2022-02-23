using Common.XO.Requests;
using Devices.Core.State.Enums;
using Devices.Core.State.Interfaces;
using System.Threading.Tasks;

namespace Devices.Core.State.Actions
{
    internal class DeviceManageStateAction : DeviceBaseStateAction
    {
        public override DeviceWorkflowState WorkflowStateType => DeviceWorkflowState.Manage;

        public DeviceManageStateAction(IDeviceStateController _) : base(_) { }

        //static private LinkDeviceActionType lastDeviceAction = LinkDeviceActionType.GetStatus;

        //static private bool hasStatus;

        public override bool DoDeviceDiscovery()
        {
            LastException = new StateException("device recovery is needed");
            _ = Error(this);
            return true;
        }

        //private async void PostRequest()
        //{
        //    if (Controller.TargetDevices != null)
        //    {
        //        await Task.Delay(10240);

        //        // DEVICE COMMAND
        //        LinkRequest linkRequest = new LinkRequest()
        //        {
        //            MessageID = RandomGenerator.BuildRandomString(12),
        //            Actions = new System.Collections.Generic.List<LinkActionRequest>()
        //            {
        //                new LinkActionRequest()
        //                {
        //                    Action = LinkAction.DALAction,
        //                    DeviceActionRequest = new LinkDeviceActionRequest()
        //                    {
        //                        DeviceAction = lastDeviceAction
        //                    },
        //                    DeviceRequest = new LinkDeviceRequest()
        //                    {
        //                        DeviceIdentifier = new XO.Device.LinkDeviceIdentifier()
        //                        {
        //                            Manufacturer = Controller.TargetDevices[0].DeviceInformation?.Manufacturer,
        //                            Model = Controller.TargetDevices[0].DeviceInformation?.Model,
        //                            SerialNumber = Controller.TargetDevices[0].DeviceInformation?.SerialNumber
        //                        }
        //                    }
        //                }
        //            }
        //        };
        //        Console.WriteLine("----------------------------------------------------------------------------------------------------");
        //        Console.WriteLine($"REQUEST: {lastDeviceAction}");
        //        Controller.SendDeviceCommand(Newtonsoft.Json.JsonConvert.SerializeObject(linkRequest));
        //        lastDeviceAction += 1;
        //        if (lastDeviceAction >= LinkDeviceActionType.GetIdentifier)
        //        {
        //            lastDeviceAction = LinkDeviceActionType.GetStatus;
        //        }
        //    }
        //}

        public override Task DoWork()
        {
            System.Diagnostics.Debug.WriteLine("DEV-WORFLOW: MANAGE --------------------------------------------------------------------------------------------------------");
            //if (!hasStatus)
            //{ 
            //    hasStatus = true;
            //    PostRequest();
            //}

            return Task.CompletedTask;
        }

        public override void RequestReceived(LinkRequest request)
        {
            Controller.SaveState(request);

            _ = Complete(this);
        }
    }
}