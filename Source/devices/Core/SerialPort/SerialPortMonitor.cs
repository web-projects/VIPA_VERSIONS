using Devices.Common;
using Devices.Common.Helpers;
using Devices.Core.SerialPort.Interfaces;
using System;
using System.Linq;
using System.Management;

namespace Devices.Core.SerialPort
{
    public class SerialPortMonitor : ISerialPortMonitor
    {
        public event ComPortEventHandler ComportEventOccured;

        private string[] serialPorts;

        private ManagementEventWatcher usbHubArrival;
        private ManagementEventWatcher usbHubRemoval;

        private ManagementEventWatcher usbSerialArrival;
        private ManagementEventWatcher usbSerialRemoval;

        public void StartMonitoring()
        {
            serialPorts = GetAvailableSerialPorts();

            // USB Port devices
            MonitorUSBSerialDevicesPortChanges();

            // USB HUB devices
            MonitorUsbHubDevicesChanges();
        }

        public void StopMonitoring() => Dispose();

        public void Dispose()
        {
            if (usbSerialArrival != null)
            {
                usbSerialArrival?.Stop();
                usbSerialArrival?.Dispose();
                usbSerialArrival = null;
            }
            if (usbSerialRemoval != null)
            {
                usbSerialRemoval?.Stop();
                usbSerialRemoval?.Dispose();
                usbSerialRemoval = null;
            }

            if (usbHubArrival != null)
            {
                usbHubArrival?.Stop();
                usbHubArrival?.Dispose();
                usbHubArrival = null;
            }
            if (usbHubRemoval != null)
            {
                usbHubRemoval?.Stop();
                usbHubRemoval?.Dispose();
                usbHubRemoval = null;
            }
        }

        private static string[] GetAvailableSerialPorts()
            => System.IO.Ports.SerialPort.GetPortNames();

        #region --- USB PORT DEVICES ----
        private void MonitorUSBSerialDevicesPortChanges()
        {
            try
            {
                // Detect insertion of all USBSerial devices - Query every 1 second for device remove/insert
                WqlEventQuery deviceArrivalQuery = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 2");
                usbSerialArrival = new ManagementEventWatcher(deviceArrivalQuery);
                usbSerialArrival.EventArrived += (sender, eventArgs) => RaiseUsbSerialPortsChangedIfNecessary(PortEventType.Insertion, eventArgs);
                // Start listening for USB device Arrival events
                usbSerialArrival.Start();

                // Detect removal of all USBSerial devices
                WqlEventQuery deviceRemovalQuery = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 3");
                usbSerialRemoval = new ManagementEventWatcher(deviceRemovalQuery);
                usbSerialRemoval.EventArrived += (sender, eventArgs) => RaiseUsbSerialPortsChangedIfNecessary(PortEventType.Removal, eventArgs);
                // Start listening for USB Removal events
                usbSerialRemoval.Start();
            }
            catch (ManagementException e)
            {
                Console.WriteLine($"serial: COMM exception={e.Message}");
            }
        }

        private void RaiseUsbSerialPortsChangedIfNecessary(PortEventType eventType, EventArrivedEventArgs eventArgs)
        {
            lock (serialPorts)
            {
                string[] availableSerialPorts = GetAvailableSerialPorts();

                if (eventType == PortEventType.Insertion)
                {
                    if (!serialPorts?.SequenceEqual(availableSerialPorts) ?? false)
                    {
                        var added = availableSerialPorts.Except(serialPorts).ToArray();
                        if (added.Length > 0)
                        {
                            serialPorts = availableSerialPorts;

                            ComportEventOccured?.Invoke(PortEventType.Insertion, added[0]);
                        }
                    }
                }
                else if (eventType == PortEventType.Removal)
                {
                    var removed = serialPorts.Except(availableSerialPorts).ToArray();
                    if (removed.Length > 0)
                    {
                        serialPorts = availableSerialPorts;

                        ComportEventOccured?.Invoke(PortEventType.Removal, removed[0]);
                    }
                }
            }
        }
        #endregion --- USB PORT DEVICES

        #region --- USB HUB DEVICES ----
        private void MonitorUsbHubDevicesChanges()
        {
            try
            {
                // Detect insertion of all USBHub devices - Query every 1 second for device remove/insert
                WqlEventQuery deviceArrivalQuery = new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBHub'");
                usbHubArrival = new ManagementEventWatcher(deviceArrivalQuery);
                usbHubArrival.EventArrived += (sender, eventArgs) => RaiseUsbHubPortsChangedIfNecessary(PortEventType.Insertion, eventArgs);
                // Start listening for events
                usbHubArrival.Start();

                // Detect removal of all USBHub devices
                WqlEventQuery deviceRemovalQuery = new WqlEventQuery("SELECT * FROM __InstanceDeletionEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBHub'");
                usbHubRemoval = new ManagementEventWatcher(deviceRemovalQuery);
                usbHubRemoval.EventArrived += (sender, eventArgs) => RaiseUsbHubPortsChangedIfNecessary(PortEventType.Removal, eventArgs);
                // Start listening for events
                usbHubRemoval.Start();
            }
            catch (ManagementException e)
            {
                Console.WriteLine($"serial: COMM exception={e.Message}");
            }
        }

        private void RaiseUsbHubPortsChangedIfNecessary(PortEventType eventType, EventArrivedEventArgs eventArgs)
        {
            lock (serialPorts)
            {
                string[] availableSerialPorts = GetAvailableSerialPorts();
                ManagementBaseObject targetObject = eventArgs.NewEvent["TargetInstance"] as ManagementBaseObject;
                string targetDeviceID = targetObject["DeviceID"]?.ToString() ?? string.Empty;
                if (targetDeviceID.Contains("vid_0801", StringComparison.OrdinalIgnoreCase))        //hardcode value for Magtek
                {
                    ComportEventOccured?.Invoke(eventType, targetDeviceID);
                    return;
                }
            }
        }
        #endregion --- MAGTEK DEVICES
    }
}
