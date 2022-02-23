using Devices.Common;
using Devices.Common.AppConfig;
using Devices.Common.Constants;
using Devices.Common.Helpers;
using Devices.Common.Interfaces;
using Devices.Core.Cancellation;
using Devices.Core.State.Enums;
using Devices.Core.State.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common.XO.Responses;
using System.Linq;
using static Devices.Common.SupportedDevices;

namespace Devices.Core.State.Actions
{
    internal class DeviceInitializeDeviceCommunicationStateAction : DeviceBaseStateAction
    {
        public override DeviceWorkflowState WorkflowStateType => DeviceWorkflowState.InitializeDeviceCommunication;

        public DeviceInitializeDeviceCommunicationStateAction(IDeviceStateController _) : base(_) { }

        private bool DeviceIsSupported(List<IPaymentDevice> availableCardDevices, IPaymentDevice targetDevice)
        {
            bool result = false;
            DeviceSection deviceSection = Controller.Configuration;

            foreach (IPaymentDevice device in availableCardDevices)
            {
                if (string.Equals(device.ManufacturerConfigID, targetDevice.ManufacturerConfigID, StringComparison.OrdinalIgnoreCase))
                {
                    switch (device.ManufacturerConfigID)
                    {
                        case IdTechManufacturerId:
                            break;

                        case VerifoneManufacturerId:
                            result = (deviceSection.Verifone.SupportedDevices.Select(x => x).Where(y => y.Equals($"{targetDevice.DeviceInformation.Manufacturer}-{targetDevice.DeviceInformation.Model}", StringComparison.OrdinalIgnoreCase)).Count() == 1);
                            break;

                        case SimulatorManufacturerId:
                            result = (deviceSection.Simulator.SupportedDevices.Select(x => x).Where(y => y.Equals($"{targetDevice.DeviceInformation.Manufacturer}-{targetDevice.DeviceInformation.Model}", StringComparison.OrdinalIgnoreCase)).Count() == 1);
                            break;

                        case NullDeviceManufacturerId:
                            break;

                        //case MagTekManufacturerId:
                        //    result = deviceSection.MagTekConfig != null;  //We don't need name validation until we have more than 1 model
                        //    break;

                        default:
                            //_ = Controller.LoggingClient.LogErrorAsync($"Unable to obtain a device reference");
                            System.Diagnostics.Debug.WriteLine("Unable to obtain a device reference");
                            break;
                    }
                    break;
                }
            }
            return result;
        }

        public override Task DoWork()
        {
            string pluginPath = Controller.PluginPath;
            List<ICardDevice> availableCardDevices = null;
            List<ICardDevice> discoveredCardDevices = null;
            List<ICardDevice> validatedCardDevices = null;

            System.Diagnostics.Debug.WriteLine("DEV-WORFLOW: initialization ------------------------------------------------------------------------------------");

            try
            {
                availableCardDevices = Controller.DevicePluginLoader.FindAvailableDevices(pluginPath);

                // filter out devices that are disabled
                if (availableCardDevices.Count > 0)
                {
                    DeviceSection deviceSection = Controller.Configuration;
                    foreach (var device in availableCardDevices)
                    {
                        switch (device.ManufacturerConfigID)
                        {
                            case "IdTech":
                            {
                                device.SortOrder = deviceSection.IdTech.SortOrder;
                                break;
                            }

                            case "Verifone":
                            {
                                device.SortOrder = deviceSection.Verifone.SortOrder;
                                break;
                            }

                            case "Simulator":
                            {
                                device.SortOrder = deviceSection.Simulator.SortOrder;
                                break;
                            }
                        }
                    }
                    availableCardDevices.RemoveAll(x => x.SortOrder == -1);

                    if (availableCardDevices?.Count > 1)
                    {
                        availableCardDevices.Sort();
                    }
                }

                // Probe validated devices
                discoveredCardDevices = new List<ICardDevice>();
                validatedCardDevices = new List<ICardDevice>();
                validatedCardDevices.AddRange(availableCardDevices);

                for (int i = validatedCardDevices.Count - 1; i >= 0; i--)
                {
                    if (string.Equals(availableCardDevices[i].ManufacturerConfigID, DeviceType.NoDevice.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    bool success = false;
                    try
                    {
                        List<DeviceInformation> deviceInformation = availableCardDevices[i].DiscoverDevices();

                        if (deviceInformation == null)
                        {
                            continue;
                        }

                        foreach (var deviceInfo in deviceInformation)
                        {
                            DeviceConfig deviceConfig = new DeviceConfig()
                            {
                                Valid = true
                            };
                            SerialDeviceConfig serialConfig = new SerialDeviceConfig
                            {
                                CommPortName = deviceInfo.ComPort
                            };
                            deviceConfig.SetSerialDeviceConfig(serialConfig);

                            ICardDevice device = validatedCardDevices[i].Clone() as ICardDevice;

                            device.DeviceEventOccured += Controller.DeviceEventReceived;

                            // Device powered on status capturing: free up the com port and try again.
                            // This occurs when a USB device repowers the USB interface and the virtual port is open.
                            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                            IDeviceCancellationBroker cancellationBroker = Controller.GetCancellationBroker();
                            Task<Polly.PolicyResult<List<LinkErrorValue>>> timeoutPolicy = cancellationBroker.ExecuteWithTimeoutAsync<List<LinkErrorValue>>(
                                _ => device.Probe(deviceConfig, deviceInfo, out success),
                                Timeouts.DALDeviceRecoveryTimeout,
                                cancellationTokenSource.Token);

                            if (timeoutPolicy.Result.Outcome == Polly.OutcomeType.Failure)
                            {
                                Console.WriteLine($"Unable to obtain device status for - '{device.Name}'.");
                                device.DeviceEventOccured -= Controller.DeviceEventReceived;
                                device?.Disconnect();
                                LastException = new StateException("Unable to find a valid device to connect to.");
                                _ = Error(this);
                                return Task.CompletedTask;
                            }
                            else if (success)
                            {
                                string deviceName = $"{device.DeviceInformation.Manufacturer}-{device.DeviceInformation.Model}";
                                if (Controller.Configuration.Verifone.SupportedDevices.Contains(deviceName))
                                {
                                    discoveredCardDevices.Add(device);
                                }
                                else
                                {
                                    device.DeviceSetIdle();
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"device: exception='{e.Message}'");

                        discoveredCardDevices[i].DeviceEventOccured -= Controller.DeviceEventReceived;

                        // Consume failures
                        if (success)
                        {
                            success = false;
                        }
                    }

                    if (success)
                    {
                        continue;
                    }

                    validatedCardDevices.RemoveAt(i);
                }
            }
            catch
            {
                availableCardDevices = new List<ICardDevice>();
            }

            if (discoveredCardDevices?.Count > 0)
            {
                Controller.SetTargetDevices(discoveredCardDevices);
            }

            if (Controller.TargetDevices != null)
            {
                foreach (ICardDevice device in Controller.TargetDevices)
                {
                    //Controller.LoggingClient.LogInfoAsync($"Device found: name='{device.Name}', model={device.DeviceInformation.Model}, " +
                    //    $"serial={device.DeviceInformation.SerialNumber}");
                    //Console.WriteLine($"DEVICE FOUND: name='{device.Name}', model='{device?.DeviceInformation?.Model}', " +
                    //    $"serial='{device?.DeviceInformation?.SerialNumber}'\n");

                    // Always publish device connect event
                    //if (!string.Equals(device.DeviceInformation.Manufacturer, Controller.TargetDevices[0].DeviceInformation?.Manufacturer, StringComparison.CurrentCultureIgnoreCase) ||
                    //    !string.Equals(device.DeviceInformation.SerialNumber, Controller.TargetDevices[0].DeviceInformation?.SerialNumber, StringComparison.CurrentCultureIgnoreCase))
                    {
                        Controller.PublishDeviceConnectEvent(device, device.DeviceInformation.ComPort);
                    }

                    Controller.DeviceStatusUpdate();
                    device.DeviceSetIdle();
                }
            }
            else
            {
                //Controller.LoggingClient.LogInfoAsync("Unable to find a valid device to connect to.");
                //Console.WriteLine("Unable to find a valid device to connect to.");
                LastException = new StateException("Unable to find a valid device to connect to.");
                _ = Error(this);
                return Task.CompletedTask;
            }

            _ = Complete(this);

            return Task.CompletedTask;
        }
    }
}
