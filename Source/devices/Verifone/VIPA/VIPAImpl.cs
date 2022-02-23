using Common.Config.Config;
using Common.Execution;
using Common.LoggerManager;
using Common.XO.Device;
using Common.XO.Private;
using Common.XO.Responses;
using Devices.Common;
using Devices.Common.AppConfig;
using Devices.Common.Config;
using Devices.Common.Helpers;
using Devices.Common.Helpers.Templates;
using Devices.Verifone.Connection;
using Devices.Verifone.Helpers;
using Devices.Verifone.VIPA.Helpers;
using Devices.Verifone.VIPA.Interfaces;
using Devices.Verifone.VIPA.TagLengthValue;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static Devices.Common.Constants.LogMessage;
using static Devices.Verifone.Helpers.Messages;

namespace Devices.Verifone.VIPA
{
    public class VIPAImpl : IVipa, IDisposable
    {
        #region --- enumerations ---
        public enum VIPADisplayMessageValue
        {
            Custom = 0x00,
            Idle = 0x01,
            ProcessingTransaction = 0x02,
            Authorising = 0x03,
            RequestRejected = 0x04,
            InsertCardWithBeeps = 0x0D,
            RemoveCardWithBeeps = 0x0E,
            Processing = 0x0F
        }

        private enum ResetDeviceCfg
        {
            ReturnSerialNumber = 1 << 0,
            ReturnAfterCardRemoval = 1 << 1,
            LeaveScreenDisplayUnchanged = 1 << 2,
            SlideShowStartsNormalTiming = 1 << 3,
            NoBeepDuringReset = 1 << 4,
            ResetImmediately = 1 << 5,
            ReturnPinpadConfiguration = 1 << 6,
            AddVOSComponentsInformation = 1 << 7
        }

        public enum ManualPANEntry
        {
            PANEntry = 1 << 0,
            ExpiryDate = 1 << 1,
            ApplicationExpirationDate = 1 << 2,
            CVV2Entry = 1 << 3
        }

        public enum ManualPANEntryMisc
        {
            Backlight = 1 << 0,
            Track2Format = 1 << 1
        }

        private enum ContactlessReaderConfig
        {
            DeviceCount = 0,
            DeviceFirmwareVersion = 1 << 0,     // Retrieve device firmware revision (tag DFC022)
            DeviceName = 1 << 1,                // Retrieve device name(tag DFC020)
            DeviceSerialNumber = 1 << 2,        // Retrieve device serial number (tag DFC021)
            DeviceInitStatus = 1 << 3,          // Retrieve device initialization status(tag DFC023)
            DeviceDispCapabilities = 1 << 4,    // Retrieve device display capabilities(tag DFC024)
            DeviceSoundCapabilities = 1 << 5,   // Retrieve device sound capabilities(tag DFC025)
            DeviceLEDCapabilities = 1 << 6,     // Retrieve device LED capabilities(tag DFC026)
            DeviceKernelInformation = 1 << 7    // Retrieve additional Kernel information(tag DFC028)
        }
        #endregion --- enumerations ---

        #region --- attributes ---

        // Optimal Packet Size for READ/WRITE operations on device
        const int PACKET_SIZE = 1024;

        private const int DefaultDeviceResultTimeoutMS = 15000;

        private int ResponseTagsHandlerSubscribed = 0;
        private int ResponseTaglessHandlerSubscribed = 0;
        private int ResponseContactlessHandlerSubscribed = 0;

        private Modes.Execution ExecutionMode { get; set; } = Modes.Execution.StandAlone;

        public DeviceInformation DeviceInformation { get; set; }

        public TaskCompletionSource<int> ResponseCodeResult = null;

        public delegate void ResponseTagsHandlerDelegate(List<TLV> tags, int responseCode, bool cancelled = false);
        internal ResponseTagsHandlerDelegate ResponseTagsHandler = null;

        public delegate void ResponseTaglessHandlerDelegate(byte[] data, int dataLength, int responseCode, bool cancelled = false);
        internal ResponseTaglessHandlerDelegate ResponseTaglessHandler = null;

        public delegate void ResponseCLessHandlerDelegate(List<TLV> tags, int responseCode, int pcb, bool cancelled = false);
        internal ResponseCLessHandlerDelegate ResponseCLessHandler = null;

        public TaskCompletionSource<(DevicePTID devicePTID, int VipaResponse)> DeviceResetConfiguration = null;

        public TaskCompletionSource<(DeviceInfoObject deviceInfoObject, int VipaResponse)> DeviceIdentifier = null;
        public TaskCompletionSource<(SecurityConfigurationObject securityConfigurationObject, int VipaResponse)> DeviceSecurityConfiguration = null;
        public TaskCompletionSource<(DeviceContactlessInfo deviceContactlessInfo, int VipaResponse)> ResponseCodeWithDataResult = null;
        public TaskCompletionSource<(KernelConfigurationObject kernelConfigurationObject, int VipaResponse)> DeviceKernelConfiguration = null;

        public TaskCompletionSource<(string HMAC, int VipaResponse)> DeviceGenerateHMAC = null;
        public TaskCompletionSource<(BinaryStatusObject binaryStatusObject, int VipaResponse)> DeviceBinaryStatusInformation = null;

        //public TaskCompletionSource<(HTMLResponseObject htmlResponseObject, int VipaResponse)> DeviceHTMLResponse = null;

        public TaskCompletionSource<(LinkDALRequestIPA5Object linkDALRequestIPA5Object, int VipaResponse)> DeviceInteractionInformation { get; set; } = null;

        private List<byte[]> signaturePayload = null;

        public TaskCompletionSource<(string Timestamp, int VipaResponse)> Reboot24HourInformation = null;

        public TaskCompletionSource<(string Timestamp, int VipaResponse)> TerminalDateTimeInformation = null;

        // EMV Workflow
        public TaskCompletionSource<(LinkDALRequestIPA5Object linkDALRequestIPA5Object, int VipaResponse)> DecisionRequiredInformation = null;

        public event DeviceLogHandler DeviceLogHandler;

        public event DeviceEventHandler DeviceEventHandler;

        #endregion --- attributes ---

        #region --- connection ---
        private VerifoneConnection VerifoneConnection { get; set; }

        public bool Connect(VerifoneConnection connection, DeviceInformation deviceInformation)
        {
            VerifoneConnection = connection;
            DeviceInformation = deviceInformation;
            return VerifoneConnection.Connect(DeviceInformation, DeviceLogHandler);
        }

        public bool IsConnected()
        {
            return VerifoneConnection?.IsConnected() ?? false;
        }

        public void Dispose()
        {
            VerifoneConnection?.Dispose();
        }

        public void ConnectionConfiguration(SerialDeviceConfig serialConfig, DeviceEventHandler deviceEventHandler, DeviceLogHandler deviceLogHandler)
        {
            DeviceLogHandler = deviceLogHandler;
            DeviceEventHandler = deviceEventHandler;

            if (serialConfig != null)
            {
                if (serialConfig.CommPortName.Length > 0)
                {
                    VerifoneConnection.serialConnection.Config.SerialConfig.CommPortName = serialConfig.CommPortName;
                }

                if (serialConfig.CommBaudRate > 0)
                {
                    VerifoneConnection.serialConnection.Config.SerialConfig.CommBaudRate = serialConfig.CommBaudRate;
                }

                if (serialConfig.CommReadTimeout > 0)
                {
                    VerifoneConnection.serialConnection.Config.SerialConfig.CommReadTimeout = serialConfig.CommReadTimeout;
                }

                if (serialConfig.CommWriteTimeout > 0)
                {
                    VerifoneConnection.serialConnection.Config.SerialConfig.CommWriteTimeout = serialConfig.CommWriteTimeout;
                }
            }
        }

        #endregion --- connection ---

        #region --- resources ---
        private bool FindEmbeddedResourceByName(string fileName, string fileTarget)
        {
            bool result = false;

            // Main Assembly contains embedded resources
            Assembly mainAssembly = Assembly.GetEntryAssembly();
            foreach (string name in mainAssembly.GetManifestResourceNames())
            {
                if (name.EndsWith(fileName, StringComparison.InvariantCultureIgnoreCase))
                {
                    using (Stream stream = mainAssembly.GetManifestResourceStream(name))
                    {
                        BinaryReader br = new BinaryReader(stream);
                        // always create working file
                        FileStream fs = File.Open(fileTarget, FileMode.Create);
                        BinaryWriter bw = new BinaryWriter(fs);
                        byte[] ba = new byte[stream.Length];
                        stream.Read(ba, 0, ba.Length);
                        bw.Write(ba);
                        br.Close();
                        bw.Close();
                        stream.Close();
                        result = true;
                    }
                    break;

                }
            }
            return result;
        }

        private bool ContactlessReaderInitialized;
        #endregion --- resources ---

        private void WriteSingleCmd(VIPACommand command)
        {
            VerifoneConnection?.WriteSingleCmd(new VIPAResponseHandlers
            {
                responsetagshandler = ResponseTagsHandler,
                responsetaglesshandler = ResponseTaglessHandler,
                responsecontactlesshandler = ResponseCLessHandler
            }, command);
        }

        private void WriteRawBytes(byte[] buffer)
        {
            VerifoneConnection?.WriteRaw(buffer, buffer.Length);
        }

        private void SendVipaCommand(VIPACommandType commandType, byte p1, byte p2, byte[] data = null, byte nad = 0x1, byte pcb = 0x0)
        {
            Debug.WriteLine($"Send VIPA {commandType}");
            VIPACommand command = new VIPACommand(commandType) { nad = nad, pcb = pcb, p1 = p1, p2 = p2, data = data };
            WriteSingleCmd(command);
        }

        private void WriteChainedCmd(VIPACommand command)
        {
            VerifoneConnection?.WriteChainedCmd(new VIPAResponseHandlers
            {
                responsetagshandler = ResponseTagsHandler,
                responsetaglesshandler = ResponseTaglessHandler,
                responsecontactlesshandler = ResponseCLessHandler
            }, command);
        }

        private void SendVipaChainedCommand(VIPACommandType commandType, byte p1, byte p2, byte[] data = null, byte nad = 0x1, byte pcb = 0x0)
        {
            Debug.WriteLine($"Send VIPA {commandType}");
            VIPACommand command = new VIPACommand(commandType) { nad = nad, pcb = pcb, p1 = p1, p2 = p2, data = data };
            WriteChainedCmd(command);
        }

        #region --- VIPA commands ---

        #region --- Utilities ---
        //private void DeviceLogger(LogLevel logLevel, string message) =>
        //    DeviceLogHandler?.Invoke(logLevel, $"{StringValueAttribute.GetStringValue(DeviceType.Verifone)}[{DeviceInformation?.Model}, {DeviceInformation?.SerialNumber}, {DeviceInformation?.ComPort}]: {{{message}}}");
        private void DeviceLogger(LogLevel logLevel, string message)
        {

        }

        #endregion --- Utilities ---

        #region --- Template Processing ---
        private void E0TemplateManualPanProcessing(LinkDALRequestIPA5Object cardResponse, TLV tag)
        {
            string panData = string.Empty;
            string cvvData = string.Empty;
            string expiryData = string.Empty;

            foreach (TLV dataTag in tag.InnerTags)
            {
                if (dataTag.Tag == E0Template.ManualPANData)
                {
                    panData = ConversionHelper.ByteArrayToHexString(dataTag.Data).Replace("A", "*");
                }
                else if (dataTag.Tag == E0Template.ManualCVVData)
                {
                    cvvData = ConversionHelper.ByteArrayToHexString(dataTag.Data).Replace("A", "*");
                }
                else if (dataTag.Tag == E0Template.ManualExpiryData)
                {
                    expiryData = ConversionHelper.ByteArrayToHexString(dataTag.Data);
                }
                //else if (dataTag.Tag == SREDTemplate.SREDTemplateTag)
                //{
                //    ProcessSREDTemplateTags(cardResponse, dataTag);
                //}
                //else if (dataTag.Tag == EmbeddedTokenization.TokenizationTemplateTag)
                //{
                //    cardResponse.CapturedEMVCardData ??= new DAL_EMVCardData();
                //    ProcessEmbeddedTokenization(cardResponse, dataTag);
                //}
            }

            //cardResponse.Track2 = string.Format(";{0}={1}{2}{3}", panData, expiryData, ServiceCodeManualInput, cvvData);
            //TODO: soften this requirement for US only through configuration
            //cardResponse.TerminalCountryCode = BitConverter.ToString(EETemplate.CountryCodeUS).Replace("-", "");
        }

        private void E1TemplateProcessing(DeviceContactlessInfo device, TLV tag)
        {
            foreach (TLV dataTag in tag.InnerTags)
            {
                if (dataTag.Tag == E1Template.DeviceName)
                {
                    device.DeviceName = Encoding.UTF8.GetString(dataTag.Data);
                }
                else if (dataTag.Tag == E1Template.SerialNumber)
                {
                    device.SerialNumber = Encoding.UTF8.GetString(dataTag.Data);
                }
                else if (dataTag.Tag == E1Template.FirmwareRevision)
                {
                    device.FirmwareRevision = Encoding.UTF8.GetString(dataTag.Data);
                }
                else if (dataTag.Tag == E1Template.InitializationStatus)
                {
                    device.InitialStatus = Encoding.UTF8.GetString(dataTag.Data);
                }
                else if (dataTag.Tag == E1Template.KernelInformation)
                {
                    device.KernelInformation = Encoding.UTF8.GetString(dataTag.Data);
                }
            }
        }

        private void EETemplateProcessing(LinkDALRequestIPA5Object cardInfo, LinkDeviceResponse deviceResponse, TLV tag)
        {
            //Verifone payment devices with multiple devices return an EE template for each device, e.g. UX301 has a UX100 pinpad.
            //The first EE template has the payment device serial #/model. Second EE is the pin pad device.

            foreach (TLV dataTag in tag.InnerTags)
            {
                if (dataTag.Tag == EETemplate.TerminalName && string.IsNullOrWhiteSpace(deviceResponse.Model))
                {
                    deviceResponse.Model = Encoding.UTF8.GetString(dataTag.Data);
                }
                else if (dataTag.Tag == EETemplate.SerialNumber && string.IsNullOrWhiteSpace(deviceResponse.SerialNumber))
                {
                    deviceResponse.SerialNumber = Encoding.UTF8.GetString(dataTag.Data);
                    DeviceInformation.SerialNumber = deviceResponse.SerialNumber ?? string.Empty;
                }
                else if (dataTag.Tag == EETemplate.TamperStatus)
                {
                    //DF8101 = 00 no tamper detected
                    //DF8101 = 01 tamper detected
                    //cardInfo.TamperStatus = BitConverter.ToString(dataTag.Data).Replace("-", "");
                }
                else if (dataTag.Tag == EETemplate.ArsStatus)
                {
                    //DF8102 = 00 ARS not active
                    //DF8102 = 01 ARS active
                    //cardInfo.ArsStatus = BitConverter.ToString(dataTag.Data).Replace("-", "");
                }
            }

            // VOS Versions: sequentially paired TAG/VALUE sets
            List<TLV> vosVersions = tag.InnerTags.Where(x => x.Tag == EFTemplate.EMVLibraryName || x.Tag == EFTemplate.EMVLibraryVersion).ToList();

            if (vosVersions is { } && vosVersions.Count > 0)
            {
                // Vault
                int vaultVersionIndex = vosVersions.FindIndex(x => Encoding.UTF8.GetString(x.Data).Equals(EFTemplate.ADKVault, StringComparison.OrdinalIgnoreCase));
                if (vaultVersionIndex != -1 && vosVersions.Count > vaultVersionIndex + 1 && vosVersions.ElementAt(vaultVersionIndex + 1).Tag == EFTemplate.EMVLibraryVersion)
                {
                    DeviceInformation.VOSVersions.ADKVault = Encoding.UTF8.GetString(vosVersions.ElementAt(vaultVersionIndex + 1).Data);
                }
                // AppManager
                int appManagerVersionIndex = vosVersions.FindIndex(x => Encoding.UTF8.GetString(x.Data).Equals(EFTemplate.ADKAppManager, StringComparison.OrdinalIgnoreCase));
                if (appManagerVersionIndex != -1 && vosVersions.Count > appManagerVersionIndex + 1 && vosVersions.ElementAt(appManagerVersionIndex + 1).Tag == EFTemplate.EMVLibraryVersion)
                {
                    DeviceInformation.VOSVersions.ADKAppManager = Encoding.UTF8.GetString(vosVersions.ElementAt(appManagerVersionIndex + 1).Data);
                }
                // OpenProtocol
                int openProtocolVersionIndex = vosVersions.FindIndex(x => Encoding.UTF8.GetString(x.Data).Equals(EFTemplate.ADKOpenProtocol, StringComparison.OrdinalIgnoreCase));
                if (openProtocolVersionIndex != -1 && vosVersions.Count > openProtocolVersionIndex + 1 && vosVersions.ElementAt(openProtocolVersionIndex + 1).Tag == EFTemplate.EMVLibraryVersion)
                {
                    DeviceInformation.VOSVersions.ADKOpenProtocol = Encoding.UTF8.GetString(vosVersions.ElementAt(openProtocolVersionIndex + 1).Data);
                }
                // SRED
                int sREDVersionIndex = vosVersions.FindIndex(x => Encoding.UTF8.GetString(x.Data).Equals(EFTemplate.ADKSRED, StringComparison.OrdinalIgnoreCase));
                if (sREDVersionIndex != -1 && vosVersions.Count > sREDVersionIndex + 1 && vosVersions.ElementAt(sREDVersionIndex + 1).Tag == EFTemplate.EMVLibraryVersion)
                {
                    DeviceInformation.VOSVersions.ADKSRED = Encoding.UTF8.GetString(vosVersions.ElementAt(sREDVersionIndex + 1).Data);
                }
            }

            //TODO: this would have to be soft-coded for other countries/currencies
            //cardInfo.TerminalCountryCode = BitConverter.ToString(EETemplate.CountryCodeUS).Replace("-", "");
        }

        #endregion --- Template Processing ---

        private void ConsoleWriteLine(string output)
        {
            if (ExecutionMode == Modes.Execution.Console)
            {
                Console.WriteLine(output);
            }
        }

        #region -- contactless reader --

        private (DeviceContactlessInfo deviceContactlessInfo, int VipaResponse) GetContactlessReaderStatus(byte readerConfig)
        {
            ResponseCodeWithDataResult = new TaskCompletionSource<(DeviceContactlessInfo, int)>();

            ResponseTaglessHandlerSubscribed++;
            ResponseTaglessHandler += ResponseCodeWithDataHandler;

            SendVipaCommand(VIPACommandType.GetContactlessStatus, readerConfig, 0x00);

            (DeviceContactlessInfo deviceContactlessInfo, int VipaResponse) deviceResponse = ResponseCodeWithDataResult.Task.Result;

            ResponseTaglessHandler -= ResponseCodeWithDataHandler;
            ResponseTaglessHandlerSubscribed--;

            return deviceResponse;
        }

        /// <summary>
        /// Force Closing contactless reader regardless of open state to avoid displaying of the UI status bar.
        /// When the contactless reader is opened and device is disconnected, there's not a way for DAL to know if the reader was opened before.
        /// By force-closing the reader, the idle screen will not display the contactless UI status bar.
        /// </summary>
        /// <returns></returns>
        public int CloseContactlessReader(bool forceClose = false)
        {
            int commandResult = (int)VipaSW1SW2Codes.Failure;

            // Close only the reader when a forms update is performed
            if (ContactlessReaderInitialized || forceClose)
            {
                ContactlessReaderInitialized = false;

                ResponseCodeResult = new TaskCompletionSource<int>();

                ResponseTagsHandlerSubscribed++;
                ResponseTagsHandler += ResponseCodeHandler;

                SendVipaCommand(VIPACommandType.CloseContactlessReader, 0x00, 0x00);   // Close CLess Reader [C0, 02]

                commandResult = ResponseCodeResult.Task.Result;

                ResponseTagsHandler -= ResponseCodeHandler;
                ResponseTagsHandlerSubscribed--;
            }

            return commandResult;
        }

        #endregion -- contactless reader --

        public bool DisplayMessage(VIPADisplayMessageValue displayMessageValue = VIPADisplayMessageValue.Idle, bool enableBacklight = false, string customMessage = "")
        {
            ResponseCodeResult = new TaskCompletionSource<int>();

            ResponseTagsHandlerSubscribed++;
            ResponseTagsHandler += ResponseCodeHandler;

            // Display [D2, 01]
            SendVipaCommand(VIPACommandType.Display, (byte)displayMessageValue, (byte)(enableBacklight ? 0x01 : 0x00), Encoding.ASCII.GetBytes(customMessage));

            int displayCommandResponseCode = ResponseCodeResult.Task.Result;

            ResponseTagsHandler -= ResponseCodeHandler;
            ResponseTagsHandlerSubscribed--;

            return displayCommandResponseCode == (int)VipaSW1SW2Codes.Success;
        }

        internal (int VipaData, int VipaResponse) DeviceCommandAbort()
        {
            (int VipaData, int VipaResponse) deviceResponse = (-1, (int)VipaSW1SW2Codes.Failure);

            ResponseCodeResult = new TaskCompletionSource<int>();

            DeviceIdentifier = new TaskCompletionSource<(DeviceInfoObject deviceInfoObject, int VipaResponse)>(TaskCreationOptions.RunContinuationsAsynchronously);
            ResponseTagsHandlerSubscribed++;
            ResponseTagsHandler += ResponseCodeHandler;

            Debug.WriteLine(ConsoleMessages.AbortCommand.GetStringValue());
            SendVipaCommand(VIPACommandType.Abort, 0x00, 0x00);

            deviceResponse = ((int)VipaSW1SW2Codes.Success, ResponseCodeResult.Task.Result);

            ResponseTagsHandler -= ResponseCodeHandler;
            ResponseTagsHandlerSubscribed--;

            return deviceResponse;
        }

        public (DeviceInfoObject deviceInfoObject, int VipaResponse) VIPARestart()
        {
            (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceResponse = (null, (int)VipaSW1SW2Codes.Failure);

            // abort previous user entries in progress
            (int VipaData, int VipaResponse) vipaResult = DeviceCommandAbort();

            if (vipaResult.VipaResponse == (int)VipaSW1SW2Codes.Success)
            {
                DeviceIdentifier = new TaskCompletionSource<(DeviceInfoObject deviceInfoObject, int VipaResponse)>(TaskCreationOptions.RunContinuationsAsynchronously);

                ResponseTagsHandlerSubscribed++;
                ResponseTagsHandler += GetDeviceInfoResponseHandler;

                // VIPA restart with beep
                byte p2 = (byte)(ResetDeviceCfg.ReturnSerialNumber);
                SendVipaCommand(VIPACommandType.ResetDevice, 0x02, p2);

                deviceResponse = GetDeviceResponse(DefaultDeviceResultTimeoutMS);

                ResponseTagsHandler -= GetDeviceInfoResponseHandler;
                ResponseTagsHandlerSubscribed--;
            }

            return deviceResponse;
        }

        public (DeviceInfoObject deviceInfoObject, int VipaResponse) DeviceCommandReset()
        {
            (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceResponse = (null, (int)VipaSW1SW2Codes.Failure);

            // abort previous user entries in progress
            (int VipaData, int VipaResponse) vipaResult = DeviceCommandAbort();

            if (vipaResult.VipaResponse == (int)VipaSW1SW2Codes.Success)
            {
                DeviceIdentifier = new TaskCompletionSource<(DeviceInfoObject deviceInfoObject, int VipaResponse)>(TaskCreationOptions.RunContinuationsAsynchronously);

                ResponseTagsHandlerSubscribed++;
                ResponseTagsHandler += GetDeviceInfoResponseHandler;

                Debug.WriteLine(ConsoleMessages.DeviceReset.GetStringValue());
                // Reset Device [D0, 00]
                SendVipaCommand(VIPACommandType.ResetDevice, 0x00, 
                    (byte)(ResetDeviceCfg.ReturnSerialNumber | ResetDeviceCfg.ReturnAfterCardRemoval | ResetDeviceCfg.ReturnPinpadConfiguration | ResetDeviceCfg.AddVOSComponentsInformation));

                deviceResponse = DeviceIdentifier.Task.Result;

                ResponseTagsHandler -= GetDeviceInfoResponseHandler;
                ResponseTagsHandlerSubscribed--;
            }

            return deviceResponse;
        }

        public (DeviceInfoObject deviceInfoObject, int VipaResponse) DeviceExtendedReset()
        {
            (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceResponse = (null, (int)VipaSW1SW2Codes.Failure);

            // abort previous user entries in progress
            (int VipaData, int VipaResponse) vipaResult = DeviceCommandAbort();

            if (vipaResult.VipaResponse == (int)VipaSW1SW2Codes.Success)
            {
                DeviceIdentifier = new TaskCompletionSource<(DeviceInfoObject deviceInfoObject, int VipaResponse)>(TaskCreationOptions.RunContinuationsAsynchronously);

                ResponseTagsHandlerSubscribed++;
                ResponseTagsHandler += GetDeviceInfoResponseHandler;

                // Bit  1 – 0 PTID in serial response
                //        – 1 PTID plus serial number (tag 9F1E) in serial response
                //        - The following flags are only taken into account when P1 = 0x00:
                // Bit  2 - 0 — Leave screen display unchanged, 1 — Clear screen display to idle display state
                // Bit  3 - 0 — Slide show starts with normal timing, 1 — Start Slide-Show as soon as possible
                // Bit  4 - 0 — No beep, 1 — Beep during reset as audio indicator
                // Bit  5 - 0 — ‘Immediate’ reset, 1 — Card Removal delayed reset
                // Bit  6 - 1 — Do not add any information in the response, except serial number if Bit 1 is set.
                // Bit  7 - 0 — Do not return PinPad configuration, 1 — return PinPad configuration (warning: it can take a few seconds)
                // Bit  8 - 1 — Add V/OS components information (Vault, OpenProtocol, OS_SRED, AppManager) to
                // response (V/OS only).
                // Bit  9 – 1 - Force contact EMV configuration reload
                // Bit 10 – 1 – Force contactless EMV configuration reload
                // Bit 11 – 1 – Force contactless CAPK reload
                // Bit 12 – 1 – Returns OS components version (requires OS supporting this feature)
                // Bit 13 - 1 - Return communication mode (tag DFA21F) (0 - SERIAL, 1 - TCPIP, 3 - USB, 4 - BT, 5
                //            - PIPE_INTERNAL, 6 - WIFI, 7 - GPRS)
                // Bit 14 - 1 - Connect to external pinpad (PP1000SEV3) and set EXTERNAL_PINPAD to ON
                // Bit 15 - 1 - Disconnect external pinpad (PP1000SEV3) and set EXTERNAL_PINPAD to OFF
                TLV dataForReset = new TLV
                {
                    Tag = E0Template.E0TemplateTag,
                    InnerTags = new List<TLV>
                    {
                        new TLV(E0Template.ResetDeviceFlags, new byte[] { 0x02, 0x0F })
                    }
                };

                byte[] dataForResetData = TLV.Encode(dataForReset);

                Debug.WriteLine(ConsoleMessages.DeviceExtendedReset.GetStringValue());
                // Reset Device [D0, 00]
                SendVipaCommand(VIPACommandType.ResetDevice, 0x00, 0x00, dataForResetData);

                deviceResponse = DeviceIdentifier.Task.Result;

                ResponseTagsHandler -= GetDeviceInfoResponseHandler;
                ResponseTagsHandlerSubscribed--;
            }

            return deviceResponse;
        }

        private (DevicePTID devicePTID, int VipaResponse) DeviceRebootWithResponse()
        {
            (DevicePTID devicePTID, int VipaResponse) deviceResponse = (null, (int)VipaSW1SW2Codes.Failure);
            DeviceResetConfiguration = new TaskCompletionSource<(DevicePTID devicePTID, int VipaResponse)>();

            ResponseTagsHandlerSubscribed++;
            ResponseTagsHandler += DeviceResetResponseHandler;

            Debug.WriteLine(ConsoleMessages.RebootDevice.GetStringValue());
            // Reset Device [D0, 00]
            SendVipaCommand(VIPACommandType.ResetDevice, 0x01, 0x03);

            deviceResponse = DeviceResetConfiguration.Task.Result;

            ResponseTagsHandler -= DeviceResetResponseHandler;
            ResponseTagsHandlerSubscribed--;

            return deviceResponse;
        }

        private (DevicePTID devicePTID, int VipaResponse) DeviceRebootWithoutResponse()
        {
            (DevicePTID devicePTID, int VipaResponse) deviceResponse = (null, (int)VipaSW1SW2Codes.Failure);

            ResponseTagsHandlerSubscribed++;
            ResponseTagsHandler += ResponseCodeHandler;

            Debug.WriteLine(ConsoleMessages.RebootDevice.GetStringValue());
            // Reset Device [D0, 00]
            SendVipaCommand(VIPACommandType.ResetDevice, 0x01, 0x00);

            ResponseCodeResult = new TaskCompletionSource<int>();

            deviceResponse = (null, (int)VipaSW1SW2Codes.Success);

            ResponseTagsHandler -= ResponseCodeHandler;
            ResponseTagsHandlerSubscribed--;

            return deviceResponse;
        }

        public (DevicePTID devicePTID, int VipaResponse) DeviceReboot()
        {
            return DeviceRebootWithoutResponse();
        }

        public (DeviceInfoObject deviceInfoObject, int VipaResponse) GetDeviceInfo()
        {
            (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceResponse = (null, (int)VipaSW1SW2Codes.Success);
            return deviceResponse;
        }

        public (LinkDALRequestIPA5Object LinkActionRequestIPA5Object, int VipaResponse) DisplayCustomScreen(string displayMessage)
        {
            Debug.WriteLine(ConsoleMessages.DisplayCustomScreen.GetStringValue());

            (int vipaResponse, int vipaData) verifyResult = VerifyAmountScreen(displayMessage);
            LinkDALRequestIPA5Object linkActionRequestIPA5Object = new LinkDALRequestIPA5Object()
            {
                DALResponseData = new LinkDALActionResponse()
                {
                    Value = verifyResult.vipaData.ToString()
                }
            };
            return (linkActionRequestIPA5Object, verifyResult.vipaResponse);
        }

        public (LinkDALRequestIPA5Object LinkActionRequestIPA5Object, int VipaResponse) DisplayCustomScreenHTML(string displayMessage)
        {
            Debug.WriteLine(ConsoleMessages.DisplayCustomScreenHTML.GetStringValue());

            (int vipaResponse, int vipaData) verifyResult = DisplayMessageHTML(displayMessage);
            LinkDALRequestIPA5Object linkActionRequestIPA5Object = new LinkDALRequestIPA5Object()
            {
                DALResponseData = new LinkDALActionResponse()
                {
                    Value = verifyResult.vipaData.ToString()
                }
            };
            return (linkActionRequestIPA5Object, verifyResult.vipaResponse);
        }

        private LinkDALRequestIPA5Object ReportVIPAVersions(Dictionary<string, (string configVersion, BinaryStatusObject.DeviceConfigurationTypes configType, string[] deviceTypes, string fileName, string fileHash, int fileSize)> configObject,
            string deviceModel, string activeCustomerId)
        {
            LinkDALRequestIPA5Object linkActionRequestIPA5Object = new LinkDALRequestIPA5Object()
            {
                DALCdbData = new DALCDBData()
                {
                    VIPAVersion = new DALBundleVersioning(),
                    EMVVersion = new DALBundleVersioning(),
                    IdleVersion = new DALBundleVersioning()
                }
            };

            const string bundleNotFound = "NONE";
            Dictionary<string, string> versions = new Dictionary<string, string>();

            foreach (var configFile in configObject)
            {
                // VIPA version matching
                if (DeviceInformation.FirmwareVersion.StartsWith(configFile.Value.configVersion, StringComparison.OrdinalIgnoreCase))
                {
                    // Device model matching
                    if (!configFile.Value.deviceTypes.Any(x => x.Contains(deviceModel.Substring(0, 4))))
                    {
                        continue;
                    }

                    // Configuration type matching for idle screens
                    if (configFile.Value.configType == BinaryStatusObject.DeviceConfigurationTypes.IdleConfiguration)
                    {
                        if (!configFile.Key.Contains(activeCustomerId))
                        {
                            continue;
                        }
                    }

                    Debug.WriteLine($"VIPA: PROCESSING FILE=[{configFile.Value.fileName}]");

                    // assume version string is not found
                    versions[configFile.Value.fileName] = bundleNotFound;

                    // GetBinaryStatus: check for access to the file
                    (BinaryStatusObject binaryStatusObject, int VipaResponse) fileStatus = GetBinaryStatus(configFile.Value.fileName);

                    // When the file cannot be accessed, VIPA returns SW1SW2 equal to 9F13
                    if (fileStatus.VipaResponse != (int)VipaSW1SW2Codes.Success)
                    {
                        Debug.WriteLine(string.Format("VIPA {0} ACCESS ERROR=0x{1:X4} - '{2}'",
                            configFile.Value.fileName, fileStatus.VipaResponse, ((VipaSW1SW2Codes)fileStatus.VipaResponse).GetStringValue()));
                        continue;
                    }
                    else if (fileStatus.binaryStatusObject?.FileSize == 0x00)
                    {
                        Debug.WriteLine(string.Format("VIPA {0} SIZE=0x0000 ERROR=0x{1:X4} - '{2}'",
                            configFile.Value.fileName, fileStatus.VipaResponse, ((VipaSW1SW2Codes)fileStatus.VipaResponse).GetStringValue()));
                        continue;
                    }

                    // SelectFile: setup for FILE OPERATIONS
                    fileStatus = SelectFileForOps(configFile.Value.fileName);

                    if (fileStatus.VipaResponse != (int)VipaSW1SW2Codes.Success)
                    {
                        Debug.WriteLine(string.Format("VIPA {0} ACCESS ERROR=0x{1:X4} - '{2}'",
                            configFile.Value.fileName, fileStatus.VipaResponse, ((VipaSW1SW2Codes)fileStatus.VipaResponse).GetStringValue()));
                        continue;
                    }

                    // GetBinaryStatus: validate file SIZE and HASH
                    //(BinaryStatusObject binaryStatusObject, int VipaResponse) fileBinaryStatus = GetBinaryStatus(configFile.Value.fileName);

                    //if (fileBinaryStatus.VipaResponse != (int)VipaSW1SW2Codes.Success)
                    //{
                    //    Debug.WriteLine(string.Format("VIPA {0} ACCESS ERROR=0x{1:X4} - '{2}'",
                    //        configFile.Value.fileName, fileStatus.VipaResponse, ((VipaSW1SW2Codes)fileStatus.VipaResponse).GetStringValue()));

                    //    // Clean up pool allocation, clearing the array
                    //    if (fileStatus.binaryStatusObject.ReadResponseBytes != null)
                    //    {
                    //        ArrayPool<byte>.Shared.Return(fileStatus.binaryStatusObject.ReadResponseBytes, true);
                    //    }

                    //    continue;
                    //}

                    // Check for size match
                    //if (fileBinaryStatus.binaryStatusObject.FileSize != configFile.Value.fileSize)
                    //{
                    //Logger.error($"VIPA: {configFile.Value.fileName} SIZE MISMATCH! - actual={fileBinaryStatus.binaryStatusObject.FileSize}");

                    // requires CustId to process proper image version
                    //if (configFile.Value.configType != BinaryStatusObject.DeviceConfigurationTypes.IdleConfiguration)
                    //{
                    //    continue;
                    //}
                    //}

                    // Check for HASH Match
                    //if (!fileBinaryStatus.binaryStatusObject.FileCheckSum.Equals(configFile.Value.fileHash, StringComparison.OrdinalIgnoreCase))
                    //{
                    //    Logger.error($"VIPA: {configFile.Value.fileName} HASH MISMATCH! - actual={fileBinaryStatus.binaryStatusObject.FileCheckSum}");
                    // requires CustId to process proper image version
                    //if (configFile.Value.configType != BinaryStatusObject.DeviceConfigurationTypes.IdleConfiguration)
                    //{
                    //    continue;
                    //}
                    //}

                    // ReadBinary: read file contents
                    fileStatus = ReadBinaryDataFromSelectedFile(0x00, (byte)fileStatus.binaryStatusObject.FileSize);

                    if (fileStatus.VipaResponse != (int)VipaSW1SW2Codes.Success)
                    {
                        Logger.error(string.Format("VIPA {0} ACCESS ERROR=0x{1:X4} - '{2}'",
                            configFile.Value.fileName, fileStatus.VipaResponse, ((VipaSW1SW2Codes)fileStatus.VipaResponse).GetStringValue()));

                        // Clean up pool allocation, clearing the array
                        if (fileStatus.binaryStatusObject.ReadResponseBytes != null)
                        {
                            ArrayPool<byte>.Shared.Return(fileStatus.binaryStatusObject.ReadResponseBytes, true);
                        }
                    }

                    //ConsoleWriteLine(", HASH MATCH");
                    versions.Remove(configFile.Value.fileName, out _);
                    versions.Add(configFile.Value.fileName,
                        Encoding.UTF8.GetString(fileStatus.binaryStatusObject.ReadResponseBytes).Replace("\0", string.Empty));
                }
            }

            // populate response appropriately
            foreach ((string key, string value) in versions)
            {
                _ = key switch
                {
                    BinaryStatusObject.VIPA_VER_FW => ProcessVersionString(linkActionRequestIPA5Object.DALCdbData.VIPAVersion, value),
                    BinaryStatusObject.VIPA_VER_EMV => ProcessVersionString(linkActionRequestIPA5Object.DALCdbData.EMVVersion, value),
                    BinaryStatusObject.VIPA_VER_IDLE => ProcessVersionString(linkActionRequestIPA5Object.DALCdbData.IdleVersion, value),
                    _ => throw new Exception($"Invalid key identifier '{key}'.")
                };
            }

            int bundleFoundcount = versions.Where(x => !x.Value.Equals(bundleNotFound)).Count();

            return linkActionRequestIPA5Object;
        }

        public LinkDALRequestIPA5Object VIPAVersions(string deviceModel, bool hmacEnabled, string activeCustomerId) => hmacEnabled switch
        {
            true => ReportVIPAVersions(BinaryStatusObject.verifoneVipaVersions, deviceModel, activeCustomerId),
            false => ReportVIPAVersions(BinaryStatusObject.sphereVipaVersions, deviceModel, activeCustomerId)
        };

        public (string Timestamp, int VipaResponse) Get24HourReboot()
        {
            return GetPCIRebootTime();
        }

        public (string Timestamp, int VipaResponse) Reboot24Hour(string timestamp)
        {
            ConsoleWriteLine($"VIPA: SET 24 HOUR REBOOT TO [{timestamp}]");
            (string Timestamp, int VipaResponse) reboot24HourInformationObject = GetPCIRebootTime();

            if (reboot24HourInformationObject.VipaResponse == (int)VipaSW1SW2Codes.Success)
            {
                if (!timestamp.Equals(reboot24HourInformationObject.Timestamp))
                {
                    reboot24HourInformationObject = SetPCIRebootTime(timestamp);
                }
            }

            return reboot24HourInformationObject;
        }

        public (string Timestamp, int VipaResponse) GetTerminalDateTime()
        {
            return GetDeviceDateTime();
        }

        private (int vipaResponse, int vipaData) VerifyAmountScreen(string displayMessage)
        {
            CancelResponseHandlers();

            string[] messageFormat = displayMessage.Split(new char[] { '|' });

            if (messageFormat.Length != 4)
            {
                return ((int)VipaSW1SW2Codes.Failure, 0);
            }

            ResponseCodeResult = new TaskCompletionSource<int>();

            ResponseTagsHandlerSubscribed++;
            ResponseTagsHandler += ResponseCodeHandler;

            List<TLV> customScreenData = new List<TLV>
            {
                new TLV
                {
                    Tag = E0Template.E0TemplateTag,
                    InnerTags = new List<TLV>
                    {
                        new TLV(E0Template.DisplayText, Encoding.ASCII.GetBytes($"\t{messageFormat[0]}")),
                        new TLV(E0Template.DisplayText, Encoding.ASCII.GetBytes($"\t{messageFormat[1]}")),
                        new TLV(E0Template.DisplayText, Encoding.ASCII.GetBytes(" ")),
                        new TLV(E0Template.DisplayText, Encoding.ASCII.GetBytes($"\t1. {messageFormat[2]}")),
                        new TLV(E0Template.DisplayText, Encoding.ASCII.GetBytes($"\t2. {messageFormat[3]}"))
                    }
                }
            };
            byte[] customScreenDataData = TLV.Encode(customScreenData);

            SendVipaCommand(VIPACommandType.DisplayText, 0x00, 0x01, customScreenDataData);

            int displayCommandResponseCode = ResponseCodeResult.Task.Result;

            ResponseTagsHandler -= ResponseCodeHandler;
            ResponseTagsHandlerSubscribed--;

            (int vipaResponse, int vipaData) commandResult = (displayCommandResponseCode, 0);

            if (displayCommandResponseCode == (int)VipaSW1SW2Codes.Success)
            {
                // Setup reader to accept user input
                DeviceInteractionInformation = new TaskCompletionSource<(LinkDALRequestIPA5Object linkDALRequestIPA5Object, int VipaResponse)>();

                ResponseTagsHandlerSubscribed++;
                ResponseTagsHandler += GetDeviceInteractionKeyboardResponseHandler;

                // Bit 0 - Enter, Cancel, Clear keys
                // Bit 1 - function keys
                // Bit 2 - numeric keys
                SendVipaCommand(VIPACommandType.KeyboardStatus, 0x07, 0x00);

                LinkDALRequestIPA5Object cardInfo = null;

                do
                {
                    cardInfo = DeviceInteractionInformation.Task.Result.linkDALRequestIPA5Object;
                    commandResult.vipaResponse = DeviceInteractionInformation.Task.Result.VipaResponse;

                    if (cardInfo?.DALResponseData?.Status?.Equals("UserKeyPressed") ?? false)
                    {
                        Debug.WriteLine($"KEY PRESSED: {cardInfo.DALResponseData.Value}");
                        ConsoleWriteLine($"KEY PRESSED: {cardInfo.DALResponseData.Value}");
                        // <O> == 1 : YES
                        // <X> == 2 : NO
                        if (cardInfo.DALResponseData.Value.Equals(DeviceKeys.KEY_1.ToString()) || cardInfo.DALResponseData.Value.Equals(DeviceKeys.KEY_OK.ToString()) ||
                            cardInfo.DALResponseData.Value.Equals(DeviceKeys.KEY_GREEN.ToString()))
                        {
                            commandResult.vipaData = 1;
                        }
                        else if (cardInfo.DALResponseData.Value.Equals(DeviceKeys.KEY_2.ToString()) || cardInfo.DALResponseData.Value.Equals(DeviceKeys.KEY_STOP.ToString()) ||
                            cardInfo.DALResponseData.Value.Equals(DeviceKeys.KEY_RED.ToString()))
                        {
                            commandResult.vipaData = 0;
                        }
                        else
                        {
                            commandResult.vipaResponse = (int)VipaSW1SW2Codes.Failure;
                            DeviceInteractionInformation = new TaskCompletionSource<(LinkDALRequestIPA5Object linkDALRequestIPA5Object, int VipaResponse)>();
                        }
                    }
                } while (commandResult.vipaResponse == (int)VipaSW1SW2Codes.Failure);

                ResponseTagsHandler -= GetDeviceInteractionKeyboardResponseHandler;
                ResponseTagsHandlerSubscribed--;
            }

            return commandResult;
        }

        private (int vipaResponse, int vipaData) DisplayMessageHTML(string displayMessage)
        {
            CancelResponseHandlers();

            byte[] htmlResource = Encoding.ASCII.GetBytes("mapp/message.html");
            byte[] message = Encoding.ASCII.GetBytes($"\t{displayMessage}");

            List<TLV> customScreenData = new List<TLV>
            {
                new TLV
                {
                    Tag = E0Template.E0TemplateTag,
                    InnerTags = new List<TLV>
                    {
                        new TLV(E0Template.HTMLResourceName, htmlResource),
                        new TLV(E0Template.HTMLKeyName, Encoding.ASCII.GetBytes("TEMPLATE_VAR0")), new TLV(E0Template.HTMLValueName, message)
                    }
                }
            };
            byte[] customScreenDataData = TLV.Encode(customScreenData);

            SendVipaCommand(VIPACommandType.DisplayHTML, 0x00, 0x01, customScreenDataData);

            (int vipaResponse, int vipaData) commandResult = (ResponseCodeResult.Task.Result, 0);

            return commandResult;
        }

        private (DeviceInfoObject deviceInfoObject, int VipaResponse) GetDeviceResponse(int timeoutMS)
        {
            (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceResponse = (null, (int)VipaSW1SW2Codes.Failure);

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            while (stopWatch.ElapsedMilliseconds < timeoutMS)
            {
                if (!DeviceIdentifier.Task.IsCompleted)
                {
                    Thread.Sleep(100);
                }
                else
                {
                    deviceResponse = DeviceIdentifier.Task.Result;
                    break;
                }
            }

            return deviceResponse;
        }

        private List<TLV> FormatE0Tag(byte[] hmackey, byte[] generated_hmackey)
        {
            return new List<TLV>
            {
                new TLV
                {
                    Tag = E0Template.E0TemplateTag,
                    InnerTags = new List<TLV>
                    {
                        new TLV(new byte[] { 0xDF, 0xEC, 0x46 }, new byte[] { 0x03 }),
                        new TLV(new byte[] { 0xDF, 0xEC, 0x2E }, hmackey),
                        new TLV(new byte[] { 0xDF, 0xED, 0x15 }, generated_hmackey)
                    }
                }
            };
        }

        private string GetCurrentKSNHMAC(int hostID, string MAC)
        {
            DeviceSecurityConfiguration = new TaskCompletionSource<(SecurityConfigurationObject securityConfigurationObject, int VipaResponse)>();

            ResponseTagsHandlerSubscribed++;
            ResponseTagsHandler += GetGeneratedHMACResponseHandler;

            var messageForHMAC = new TLV
            {
                Tag = E0Template.E0TemplateTag,
                InnerTags = new List<TLV>
                {
                    new TLV(E0Template.MACGenerationData, ConversionHelper.HexToByteArray(MAC) ),
                    new TLV(E0Template.MACHostId, new byte[] { Convert.ToByte(hostID) })
                }
            };
            byte[] dataForHMACData = TLV.Encode(messageForHMAC);

            Debug.WriteLine(ConsoleMessages.UpdateHMACKeys.GetStringValue());
            // Generate HMAC [C4, 22]
            SendVipaCommand(VIPACommandType.GenerateHMAC, 0x00, 0x00, dataForHMACData);

            var deviceSecurityConfigurationInfo = DeviceSecurityConfiguration.Task.Result;

            ResponseTagsHandler -= GetGeneratedHMACResponseHandler;
            ResponseTagsHandlerSubscribed--;

            return deviceSecurityConfigurationInfo.securityConfigurationObject.GeneratedHMAC;
        }

        private int UpdateHMACKey(byte keyId, byte[] dataForHMACData)
        {
            ResponseCodeResult = new TaskCompletionSource<int>();

            ResponseTagsHandlerSubscribed++;
            ResponseTagsHandler += ResponseCodeHandler;

            Debug.WriteLine(ConsoleMessages.UpdateHMACKeys.GetStringValue());
            // Update Key [C4, 0A]
            SendVipaCommand(VIPACommandType.UpdateKey, 0x00, 0x00, dataForHMACData);

            int vipaResponse = ResponseCodeResult.Task.Result;

            ResponseTagsHandler -= ResponseCodeHandler;
            ResponseTagsHandlerSubscribed--;

            return vipaResponse;
        }

        private (BinaryStatusObject binaryStatusObject, int VipaResponse) GetBinaryStatus(string fileName)
        {
            CancelResponseHandlers();

            ResponseTagsHandlerSubscribed++;
            ResponseTagsHandler += GetBinaryStatusResponseHandler;

            DeviceBinaryStatusInformation = new TaskCompletionSource<(BinaryStatusObject binaryStatusObject, int VipaResponse)>();

            var data = Encoding.ASCII.GetBytes(fileName);
            byte reportMD5 = 0x80;

            // Get Binary Status [00, C0]
            SendVipaCommand(VIPACommandType.GetBinaryStatus, 0x00, reportMD5, Encoding.ASCII.GetBytes(fileName));

            var deviceBinaryStatus = DeviceBinaryStatusInformation.Task.Result;

            ResponseTagsHandler -= GetBinaryStatusResponseHandler;
            ResponseTagsHandlerSubscribed--;

            return deviceBinaryStatus;
        }

        private (BinaryStatusObject binaryStatusObject, int VipaResponse) SelectFileForOps(string fileName)
        {
            CancelResponseHandlers();

            ResponseTagsHandlerSubscribed++;
            ResponseTagsHandler += GetBinaryStatusResponseHandler;

            // When the file cannot be accessed, VIPA returns SW1SW2 equal to 9F13
            DeviceBinaryStatusInformation = new TaskCompletionSource<(BinaryStatusObject binaryStatusObject, int VipaResponse)>();

            var data = Encoding.ASCII.GetBytes(fileName);

            // Bit 2:  1 - Selection by DF name
            // Select File [00, A4]
            SendVipaCommand(VIPACommandType.SelectFile, 0x04, 0x00, Encoding.ASCII.GetBytes(fileName));

            var deviceBinaryStatus = DeviceBinaryStatusInformation.Task.Result;

            ResponseTagsHandler -= GetBinaryStatusResponseHandler;
            ResponseTagsHandlerSubscribed--;

            return deviceBinaryStatus;
        }

        private (BinaryStatusObject binaryStatusObject, int VipaResponse) ReadBinaryDataFromSelectedFile(byte readOffset, byte bytesToRead)
        {
            CancelResponseHandlers();

            ResponseTagsHandlerSubscribed++;
            ResponseTaglessHandler += GetBinaryDataResponseHandler;

            // When the file cannot be accessed, VIPA returns SW1SW2 equal to 9F13
            DeviceBinaryStatusInformation = new TaskCompletionSource<(BinaryStatusObject binaryStatusObject, int VipaResponse)>();

            // P1 bit 8 = 0: P1 and P2 are the offset at which to read the data from (15-bit addressing)
            // P1 bit 8 = 1: data size 2 bytes, first byte is low-order offset byte, 2nd byte is number of bytes to read
            // DATA - If P1 bit 8 = 0, data size 1 byte, contains the number of bytes to read
            //VIPACommand command = new VIPACommand { nad = 0x01, pcb = 0x00, cla = 0x00, ins = 0xB0, p1 = 0x00, p2 = readOffset };
            //command.includeLE = true;
            //command.le = bytesToRead;
            VIPACommand command = new VIPACommand(VIPACommandType.ReadBinary) { nad = 0x1, pcb = 0x00, p1 = 0x00, p2 = readOffset, includeLE = true, le = bytesToRead };
            // Read Binary [00, B0]
            WriteSingleCmd(command);

            var deviceBinaryStatus = DeviceBinaryStatusInformation.Task.Result;

            ResponseTaglessHandler -= GetBinaryDataResponseHandler;
            ResponseTagsHandlerSubscribed--;

            return deviceBinaryStatus;
        }

        private int StartKeyboardReader()
        {
            CancelResponseHandlers();

            ResponseCodeResult = new TaskCompletionSource<int>();

            ResponseTagsHandlerSubscribed++;
            ResponseTagsHandler += ResponseCodeHandler;

            // Setup reader to accept user input
            DeviceInteractionInformation = new TaskCompletionSource<(LinkDALRequestIPA5Object linkDALRequestIPA5Object, int VipaResponse)>();

            ResponseTagsHandlerSubscribed++;
            ResponseTagsHandler += GetDeviceInteractionKeyboardResponseHandler;

            // collect response from user
            // Bit 0 - Enter, Cancel, Clear keys
            // Bit 1 - function keys
            // Bit 2 - numeric keys
            //SendVipaCommand(VIPACommandType.KeyboardStatus, 0x07, 0x00);
            Debug.WriteLine(ConsoleMessages.KeyboardStatus.GetStringValue());
            // Keyboard Status [D0, 00]
            SendVipaCommand(VIPACommandType.KeyboardStatus, 0x07, 0x00);

            return ResponseCodeResult.Task.Result;
        }

        private int StopKeyboardReader()
        {
            if (ResponseTagsHandlerSubscribed > 0)
            {
                //SendVipaCommand(VIPACommandType.KeyboardStatus, 0x00, 0x00);
                Debug.WriteLine(ConsoleMessages.KeyboardStatus.GetStringValue());
                // Keyboard Status [D0, 61]
                SendVipaCommand(VIPACommandType.KeyboardStatus, 0x00, 0x00);

                int response = DeviceInteractionInformation.Task.Result.VipaResponse;

                ResponseTagsHandler -= GetDeviceInteractionKeyboardResponseHandler;
                ResponseTagsHandlerSubscribed--;

                return response;
            }

            return (int)VipaSW1SW2Codes.Failure;
        }

        private (string Timestamp, int VipaResponse) GetPCIRebootTime()
        {
            CancelResponseHandlers();

            ResponseTagsHandlerSubscribed++;
            ResponseTagsHandler += Get24HourRebootResponseHandler;

            Reboot24HourInformation = new TaskCompletionSource<(string Timestamp, int VipaResponse)>();

            SendVipaCommand(VIPACommandType.Terminal24HourReboot, 0x00, 0x00);

            var device24HourStatus = Reboot24HourInformation.Task.Result;

            ResponseTagsHandler -= Get24HourRebootResponseHandler;
            ResponseTagsHandlerSubscribed--;

            return device24HourStatus;
        }

        private (string Timestamp, int VipaResponse) SetPCIRebootTime(string timestamp)
        {
            CancelResponseHandlers();

            ResponseTagsHandlerSubscribed++;
            ResponseTagsHandler += Get24HourRebootResponseHandler;

            Reboot24HourInformation = new TaskCompletionSource<(string Timestamp, int VipaResponse)>();

            byte[] timestampForRebootData = Encoding.ASCII.GetBytes(timestamp);

            SendVipaCommand(VIPACommandType.Terminal24HourReboot, 0x00, 0x00, timestampForRebootData);

            var device24HourStatus = Reboot24HourInformation.Task.Result;

            ResponseTagsHandler -= Get24HourRebootResponseHandler;
            ResponseTagsHandlerSubscribed--;

            return device24HourStatus;
        }

        private (string Timestamp, int VipaResponse) GetDeviceDateTime()
        {
            CancelResponseHandlers();

            ResponseTagsHandlerSubscribed++;
            ResponseTaglessHandler += GetTerminalDateTimeResponseHandler;

            TerminalDateTimeInformation = new TaskCompletionSource<(string Timestamp, int VipaResponse)>();

            SendVipaCommand(VIPACommandType.TerminalSetGetDataTime, 0x01, 0x00);

            (string Timestamp, int VipaResponse) deviceDateTimeStatus = TerminalDateTimeInformation.Task.Result;

            ResponseTaglessHandler -= GetTerminalDateTimeResponseHandler;
            ResponseTagsHandlerSubscribed--;

            return deviceDateTimeStatus;
        }

        private int SetDeviceDateTime(string timestamp)
        {
            CancelResponseHandlers();

            ResponseCodeResult = new TaskCompletionSource<int>();

            ResponseTagsHandlerSubscribed++;
            ResponseTagsHandler += ResponseCodeHandler;

            byte[] timestampData = Encoding.ASCII.GetBytes(timestamp);

            SendVipaCommand(VIPACommandType.TerminalSetGetDataTime, 0x00, 0x00, timestampData);

            int deviceCommandResponseCode = ResponseCodeResult.Task.Result;

            ResponseTagsHandler -= ResponseCodeHandler;
            ResponseTagsHandlerSubscribed--;

            return deviceCommandResponseCode;
        }

        private int ProcessVersionString(DALBundleVersioning bundle, string value)
        {
            if (!value.Equals("NONE", StringComparison.OrdinalIgnoreCase))
            {
                string[] elements = value.Split(new char[] { '.' });

                if (elements.Length != 9)
                {
                    return (int)VipaSW1SW2Codes.Failure;
                }

                bundle.Signature = elements[(int)VerifoneSchemaIndex.Sig];
                bundle.Application = elements[((int)VerifoneSchemaIndex.App)];
                bundle.Type = elements[(int)VerifoneSchemaIndex.Type];
                bundle.TerminalType = elements[(int)VerifoneSchemaIndex.TerminalType];
                bundle.FrontEnd = elements[(int)VerifoneSchemaIndex.FrontEnd];
                bundle.Entity = elements[(int)VerifoneSchemaIndex.Entity];
                bundle.Model = elements[(int)VerifoneSchemaIndex.Model];
                bundle.Version = elements[(int)VerifoneSchemaIndex.Version];
                bundle.DateCode = elements[(int)VerifoneSchemaIndex.DateCode];
            }
            return (int)VipaSW1SW2Codes.Success;
        }

        #endregion --- VIPA commands ---

        #region --- response handlers ---

        public void CancelResponseHandlers(int retries = 1)
        {
            int count = 0;

            while (ResponseTagsHandlerSubscribed != 0 && count++ <= retries)
            {
                ResponseTagsHandler?.Invoke(null, (int)VipaSW1SW2Codes.Success, true);
                Thread.Sleep(1);
            }
            //count = 0;
            //while (ResponseTaglessHandlerSubscribed != 0 && count++ <= retries)
            //{
            //    ResponseTaglessHandler?.Invoke(null, -1, true);
            //    Thread.Sleep(1);
            //}
            //count = 0;
            //while (ResponseContactlessHandlerSubscribed != 0 && count++ <= retries)
            //{
            //    ResponseCLessHandler?.Invoke(null, -1, 0, true);
            //    Thread.Sleep(1);
            //}

            ResponseTagsHandlerSubscribed = 0;
            ResponseTagsHandler = null;
            //ResponseTaglessHandlerSubscribed = 0;
            ResponseTaglessHandler = null;
            //ResponseContactlessHandlerSubscribed = 0;
            ResponseCLessHandler = null;
        }

        public void ResponseCodeHandler(List<TLV> tags, int responseCode, bool cancelled = false)
        {
            ResponseCodeResult?.TrySetResult(cancelled ? -1 : responseCode);
        }

        public void ResponseCodeWithDataHandler(byte[] data, int dataLength, int responseCode, bool cancelled = false)
        {
            if (cancelled)
            {
                ResponseCodeWithDataResult?.TrySetResult((null, -1));
                return;
            }

            if (responseCode == (int)VipaSW1SW2Codes.Success && dataLength > 0)
            {
                if (dataLength == 1)
                {
                    byte[] array = new byte[] { data[0], 0x00 };
                    ResponseCodeWithDataResult?.TrySetResult((null, BitConverter.ToUInt16(array, 0)));
                }
                else
                {
                    List<TLV> tags = TLV.Decode(data, 0, dataLength, E1Template.E1TemplateTag);

                    DeviceContactlessInfo device = new DeviceContactlessInfo();

                    foreach (TLV tag in tags)
                    {
                        if (tag.Tag == E1Template.E1TemplateTag)
                        {
                            E1TemplateProcessing(device, tag);
                        }
                    }

                    ResponseCodeWithDataResult?.TrySetResult((device, responseCode));
                }
            }
            else
            {
                // log error responses for device troubleshooting purposes
                if (responseCode != (int)VipaSW1SW2Codes.Success)
                {
                    DeviceLogger(LogLevel.Error, string.Format("VIPA STATUS CODE=0x{0:X4} - HANDLER 001", responseCode));
                    ResponseCodeWithDataResult?.TrySetResult((null, 0));
                }
            }
        }

        public void DeviceResetResponseHandler(List<TLV> tags, int responseCode, bool cancelled = false)
        {
            if (cancelled || tags == null)
            {
                DeviceResetConfiguration?.TrySetResult((null, responseCode));
                return;
            }

            var deviceResponse = new DevicePTID();

            if (tags.FirstOrDefault().Tag == EETemplate.TerminalId)
            {
                deviceResponse.PTID = BitConverter.ToString(tags.FirstOrDefault().Data).Replace("-", "");
            }

            if (responseCode == (int)VipaSW1SW2Codes.Success)
            {
                if (tags.Count == 1)
                {
                    DeviceResetConfiguration?.TrySetResult((deviceResponse, responseCode));
                }
            }
            else
            {
                DeviceResetConfiguration?.TrySetResult((null, responseCode));
            }
        }

        private void GetDeviceInfoResponseHandler(List<TLV> tags, int responseCode, bool cancelled = false)
        {
            if (cancelled)
            {
                DeviceIdentifier?.TrySetResult((null, responseCode));
                return;
            }

            var deviceResponse = new LinkDeviceResponse
            {
                // TODO: rework to be values reflecting actual device capabilities
                /*CardWorkflowControls = new XO.Common.DAL.LinkCardWorkflowControls
                {
                    CardCaptureTimeout = 90,
                    ManualCardTimeout = 5,
                    DebitEnabled = false,
                    EMVEnabled = false,
                    ContactlessEnabled = false,
                    ContactlessEMVEnabled = false,
                    CVVEnabled = false,
                    VerifyAmountEnabled = false,
                    AVSEnabled = false,
                    SignatureEnabled = false
                }*/
            };

            LinkDALRequestIPA5Object cardInfo = new LinkDALRequestIPA5Object();

            foreach (var tag in tags)
            {
                if (tag.Tag == EETemplate.EETemplateTag)
                {
                    EETemplateProcessing(cardInfo, deviceResponse, tag);
                }
                else if (tag.Tag == EETemplate.TerminalId)
                {
                    //deviceResponse.TerminalId = Encoding.UTF8.GetString(tag.Data);
                }
                else if (tag.Tag == EFTemplate.EFTemplateTag)
                {
                    bool isEMVKernel = false;
                    foreach (var dataTag in tag.InnerTags)
                    {
                        if (dataTag.Tag == EFTemplate.WhiteListHash)
                        {
                            //cardInfo.WhiteListHash = BitConverter.ToString(dataTag.Data).Replace("-", "");
                        }
                        else if (dataTag.Tag == EFTemplate.FirmwareVersion && string.IsNullOrWhiteSpace(deviceResponse.FirmwareVersion))
                        {
                            deviceResponse.FirmwareVersion = Encoding.UTF8.GetString(dataTag.Data);
                        }
                        else if (dataTag.Tag == EFTemplate.EMVLibraryName)
                        {
                            string libraryName = Encoding.UTF8.GetString(dataTag.Data);
                            isEMVKernel = libraryName.Equals(EFTemplate.ADKEVMKernel, StringComparison.OrdinalIgnoreCase);
                        }
                        else if (dataTag.Tag == EFTemplate.EMVLibraryVersion && isEMVKernel)
                        {
                            DeviceInformation.EMVL2KernelVersion = Encoding.UTF8.GetString(dataTag.Data);
                            isEMVKernel = false;
                        }
                    }
                }
                else if (tag.Tag == E6Template.E6TemplateTag)
                {
                    deviceResponse.PowerOnNotification = new LinkDevicePowerOnNotification();

                    var _tags = TLV.Decode(tag.Data, 0, tag.Data.Length);

                    foreach (var dataTag in _tags)
                    {
                        if (dataTag.Tag == E6Template.TransactionStatus)
                        {
                            deviceResponse.PowerOnNotification.TransactionStatus = BCDConversion.BCDToInt(dataTag.Data);
                        }
                        else if (dataTag.Tag == E6Template.TransactionStatusMessage)
                        {
                            deviceResponse.PowerOnNotification.TransactionStatusMessage = Encoding.UTF8.GetString(dataTag.Data);
                        }
                        else if (dataTag.Tag == EETemplate.TerminalId)
                        {
                            deviceResponse.PowerOnNotification.TerminalID = Encoding.UTF8.GetString(dataTag.Data);
                        }
                    }
                }
            }

            if (responseCode == (int)VipaSW1SW2Codes.Success)
            {
                if (tags?.Count > 0)
                {
                    DeviceInfoObject deviceInfoObject = new DeviceInfoObject
                    {
                        LinkDeviceResponse = deviceResponse,
                        LinkDALRequestIPA5Object = cardInfo
                    };
                    deviceInfoObject.LinkDeviceResponse.Port = DeviceInformation.ComPort;

                    DeviceIdentifier?.TrySetResult((deviceInfoObject, responseCode));
                }
                //else
                //{
                //    deviceIdentifier?.TrySetResult((null, responseCode));
                //}
            }
        }

        public void GetSecurityInformationResponseHandler(List<TLV> tags, int responseCode, bool cancelled = false)
        {
            if (cancelled || tags == null)
            {
                DeviceSecurityConfiguration?.TrySetResult((null, responseCode));
                return;
            }

            var deviceResponse = new SecurityConfigurationObject();

            foreach (var tag in tags)
            {
                if (tag.Tag == E0Template.E0TemplateTag)
                {
                    foreach (var dataTag in tag.InnerTags)
                    {
                        if (dataTag.Tag == E0Template.OnlinePINKSN)
                        {
                            if (DeviceInformation.ConfigurationHostId == VerifoneSettingsSecurityConfiguration.ConfigurationHostId)
                            {
                                string ksn = ConversionHelper.ByteArrayCodedHextoString(dataTag.Data);
                                deviceResponse.OnlinePinKSN = ksn.PadLeft(20, 'F');
                            }
                            else
                            {
                                deviceResponse.OnlinePinKSN = BitConverter.ToString(dataTag.Data).Replace("-", "");
                            }
                        }
                        if (dataTag.Tag == E0Template.KeySlotNumber)
                        {
                            deviceResponse.KeySlotNumber = BitConverter.ToString(dataTag.Data).Replace("-", "");
                        }
                        else if (dataTag.Tag == E0Template.SRedCardKSN)
                        {
                            deviceResponse.SRedCardKSN = BitConverter.ToString(dataTag.Data).Replace("-", "");
                        }
                        else if (dataTag.Tag == E0Template.InitVector)
                        {
                            deviceResponse.InitVector = BitConverter.ToString(dataTag.Data).Replace("-", "");
                        }
                        else if (dataTag.Tag == E0Template.EncryptedKeyCheck)
                        {
                            deviceResponse.EncryptedKeyCheck = BitConverter.ToString(dataTag.Data).Replace("-", "");
                        }
                    }
                }
            }

            if (responseCode == (int)VipaSW1SW2Codes.Success)
            {
                if (tags.Count > 0)
                {
                    DeviceSecurityConfiguration?.TrySetResult((deviceResponse, responseCode));
                }
            }
            else
            {
                DeviceSecurityConfiguration?.TrySetResult((null, responseCode));
            }
        }

        public void GetKernelInformationResponseHandler(List<TLV> tags, int responseCode, bool cancelled = false)
        {
            if (cancelled || tags == null)
            {
                DeviceKernelConfiguration?.TrySetResult((null, responseCode));
                return;
            }

            var deviceResponse = new KernelConfigurationObject();

            foreach (var tag in tags)
            {
                // note: we just need the first instance
                if (tag.Tag == E0Template.E0TemplateTag)
                {
                    var kernelApplicationTag = tag.InnerTags.Where(x => x.Tag == E0Template.ApplicationAID).FirstOrDefault();
                    deviceResponse.ApplicationIdentifierTerminal = BitConverter.ToString(kernelApplicationTag.Data).Replace("-", "");
                    var kernelChecksumTag = tag.InnerTags.Where(x => x.Tag == E0Template.KernelConfiguration).FirstOrDefault();
                    deviceResponse.ApplicationKernelInformation = ConversionHelper.ByteArrayToAsciiString(kernelChecksumTag.Data).Replace("\0", string.Empty);
                    break;
                }
            }

            if (responseCode == (int)VipaSW1SW2Codes.Success)
            {
                if (tags.Count > 0)
                {
                    DeviceKernelConfiguration?.TrySetResult((deviceResponse, responseCode));
                }
            }
            else
            {
                DeviceKernelConfiguration?.TrySetResult((null, responseCode));
            }
        }

        public void GetGeneratedHMACResponseHandler(List<TLV> tags, int responseCode, bool cancelled = false)
        {
            if (cancelled || tags == null)
            {
                DeviceSecurityConfiguration?.TrySetResult((null, responseCode));
                return;
            }

            var deviceResponse = new SecurityConfigurationObject();

            if (tags[0].Tag == E0Template.Cryptogram)
            {
                deviceResponse.GeneratedHMAC = BitConverter.ToString(tags.FirstOrDefault().Data).Replace("-", "");
            }

            if (responseCode == (int)VipaSW1SW2Codes.Success)
            {
                if (tags.Count == 1)
                {
                    DeviceSecurityConfiguration?.TrySetResult((deviceResponse, responseCode));
                }
            }
            else
            {
                DeviceSecurityConfiguration?.TrySetResult((null, responseCode));
            }
        }

        public void GetBinaryStatusResponseHandler(List<TLV> tags, int responseCode, bool cancelled = false)
        {
            if (cancelled || tags == null)
            {
                DeviceBinaryStatusInformation?.TrySetResult((null, responseCode));
                return;
            }

            var deviceResponse = new BinaryStatusObject();

            foreach (var tag in tags)
            {
                if (tag.Tag == _6FTemplate._6fTemplateTag)
                {
                    var _tags = TLV.Decode(tag.Data, 0, tag.Data.Length);

                    foreach (var dataTag in _tags)
                    {
                        if (dataTag.Tag == _6FTemplate.FileSizeTag)
                        {
                            deviceResponse.FileSize = BCDConversion.BCDToInt(dataTag.Data);
                        }
                        else if (dataTag.Tag == _6FTemplate.FileCheckSumTag)
                        {
                            deviceResponse.FileCheckSum = BitConverter.ToString(dataTag.Data, 0).Replace("-", "");
                        }
                        else if (dataTag.Tag == _6FTemplate.SecurityStatusTag)
                        {
                            deviceResponse.SecurityStatus = BCDConversion.BCDToInt(dataTag.Data);
                        }
                    }

                    break;
                }
            }

            if (responseCode == (int)VipaSW1SW2Codes.Success)
            {
                // command could return just a response without tags
                DeviceBinaryStatusInformation?.TrySetResult((deviceResponse, responseCode));
            }
            else
            {
                deviceResponse.FileNotFound = true;
                DeviceBinaryStatusInformation?.TrySetResult((deviceResponse, responseCode));
            }
        }

        public void GetBinaryDataResponseHandler(byte[] data, int dataLength, int responseCode, bool cancelled = false)
        {
            if (cancelled)
            {
                DeviceBinaryStatusInformation?.TrySetResult((null, responseCode));
                return;
            }

            var deviceResponse = new BinaryStatusObject();

            if (responseCode == (int)VipaSW1SW2Codes.Success && data?.Length > 0)
            {
                deviceResponse.ReadResponseBytes = ArrayPool<byte>.Shared.Rent(data.Length);
                Array.Copy(data, 0, deviceResponse.ReadResponseBytes, 0, data.Length);
            }
            else
            {
                deviceResponse.FileNotFound = true;
            }

            DeviceBinaryStatusInformation?.TrySetResult((deviceResponse, responseCode));
        }

        public void GetSignatureResponseHandler(List<TLV> tags, int responseCode, bool cancelled = false)
        {
            if (cancelled)
            {
                int response = responseCode == (int)VipaSW1SW2Codes.Success ? (int)VipaSW1SW2Codes.UserEntryCancelled : responseCode;
                DeviceInteractionInformation?.TrySetResult((null, response));
                return;
            }

            bool okButtonPressed = false;
            bool collectPoints = false;
            LinkDALRequestIPA5Object deviceResponse = new LinkDALRequestIPA5Object();
            deviceResponse.SignatureData = new List<byte[]>();

            if (responseCode == (int)VipaSW1SW2Codes.Success && tags != null && tags.Count > 0)
            {
                foreach (TLV tag in tags)
                {
                    if (tag.Tag == SignatureTemplate.HTMLKey)
                    {
                        string signatureName = Encoding.UTF8.GetString(tag.Data).Replace("-", "");
                        if (signatureName.Equals("signatureTwo", StringComparison.OrdinalIgnoreCase))
                        {
                            deviceResponse.SignatureName = Encoding.UTF8.GetString(tag.Data).Replace("-", "");
                            collectPoints = true;
                        }
                    }
                    else if (tag.Tag == SignatureTemplate.HTMLValue && tag.Data.Length > 0 && collectPoints)
                    {
                        collectPoints = false;
                        byte[] worker = ArrayPool<byte>.Shared.Rent(tag.Data.Length);
                        Array.Copy(tag.Data, 0, worker, 0, tag.Data.Length);
                        deviceResponse.SignatureData.Add(worker);
                    }
                    else if (tag.Tag == SignatureTemplate.HTMLResponse)
                    {
                        int responseStatus = BCDConversion.BCDToInt(tag.Data);
                        if (responseStatus == 0)
                        {
                            okButtonPressed = true;
                        }
                        else if (responseStatus == (int)DeviceKeys.KEY_CORR)
                        {
                            DeviceInteractionInformation?.TrySetResult((null, (int)VipaSW1SW2Codes.UserEntryCorrected));
                            return;
                        }
                        else
                        {
                            DeviceInteractionInformation?.TrySetResult((null, (int)VipaSW1SW2Codes.UserEntryCancelled));
                            return;
                        }
                    }
                }
            }

            if (responseCode == (int)VipaSW1SW2Codes.Success)
            {
                if (tags.Count > 0)
                {
                    if (deviceResponse.SignatureData is { } && deviceResponse.SignatureData.Count > 0)
                    {
                        if (Buffer.ByteLength(deviceResponse.SignatureData[0]) > 0)
                        {
                            signaturePayload = deviceResponse.SignatureData;
                            DeviceInteractionInformation?.TrySetResult((deviceResponse, responseCode));
                        }
                    }
                    else if (okButtonPressed)
                    {
                        DeviceInteractionInformation?.TrySetResult((null, (int)VipaSW1SW2Codes.DataMissing));
                    }
                }
            }
            else
            {
                // log error responses for device troubleshooting purposes
                //DeviceLogger(LogLevel.Error, string.Format("VIPA STATUS CODE=0x{0:X4}", responseCode));
                Debug.WriteLine(string.Format("VIPA STATUS CODE=0x{0:X4}", responseCode));
                DeviceInteractionInformation?.TrySetResult((null, responseCode));
            }
        }

        public void SignatureEntryStatusHandler(List<TLV> tags, int responseCode, bool cancelled = false)
        {
            if (cancelled)
            {
                int response = responseCode == (int)VipaSW1SW2Codes.Success ? (int)VipaSW1SW2Codes.UserEntryCancelled : responseCode;
                DeviceInteractionInformation?.TrySetResult((null, response));
                return;
            }

            bool okButtonPressed = false;
            bool collectPoints = false;
            LinkDALRequestIPA5Object deviceResponse = new LinkDALRequestIPA5Object()
            {
                SignatureData = new List<byte[]>()
            };

            if (responseCode == (int)VipaSW1SW2Codes.Success && tags != null && tags.Count > 0)
            {
                foreach (TLV tag in tags)
                {
                    if (tag.Tag == SignatureTemplate.HTMLKey)
                    {
                        string signatureName = Encoding.UTF8.GetString(tag.Data).Replace("-", "");
                        if (signatureName.Equals("signatureTwo", StringComparison.OrdinalIgnoreCase))
                        {
                            deviceResponse.SignatureName = Encoding.UTF8.GetString(tag.Data).Replace("-", "");
                            collectPoints = true;
                        }
                    }
                    else if (tag.Tag == SignatureTemplate.HTMLValue && tag.Data.Length > 0 && collectPoints)
                    {
                        collectPoints = false;
                        byte[] workerBuffer = ArrayPool<byte>.Shared.Rent(tag.Data.Length);
                        Array.Copy(tag.Data, 0, workerBuffer, 0, tag.Data.Length);
                        deviceResponse.SignatureData.Add(workerBuffer);
                    }
                    else if (tag.Tag == SignatureTemplate.HTMLResponse)
                    {
                        int responseStatus = BCDConversion.BCDToInt(tag.Data);
                        if (responseStatus == 0)
                        {
                            okButtonPressed = true;
                        }
                        else if (responseStatus == (int)DeviceKeys.KEY_CORR)
                        {
                            DeviceInteractionInformation?.TrySetResult((null, (int)VipaSW1SW2Codes.UserEntryCorrected));
                            return;
                        }
                        else
                        {
                            DeviceInteractionInformation?.TrySetResult((null, (int)VipaSW1SW2Codes.UserEntryCancelled));
                            return;
                        }
                    }
                }
            }

            if (responseCode == (int)VipaSW1SW2Codes.Success)
            {
                if (tags.Count > 0)
                {
                    if (deviceResponse.SignatureData is { } && deviceResponse.SignatureData.Count > 0)
                    {
                        if (Buffer.ByteLength(deviceResponse.SignatureData[0]) > 0)
                        {
                            signaturePayload = deviceResponse.SignatureData;
                            DeviceInteractionInformation?.TrySetResult((deviceResponse, responseCode));
                        }
                    }
                    else if (okButtonPressed)
                    {
                        DeviceInteractionInformation?.TrySetResult((null, (int)VipaSW1SW2Codes.DataMissing));
                    }
                }
            }
            else
            {
                // log error responses for device troubleshooting purposes
                //DeviceLogger(LogLevel.Error, string.Format("VIPA STATUS CODE=0x{0:X4}", responseCode));
                DeviceInteractionInformation?.TrySetResult((null, responseCode));
            }
        }

        public void GetDeviceInteractionKeyboardResponseHandler(List<TLV> tags, int responseCode, bool cancelled = false)
        {
            bool returnResponse = false;

            if ((cancelled || tags == null) && (responseCode != (int)VipaSW1SW2Codes.CommandCancelled) &&
                (responseCode != (int)VipaSW1SW2Codes.UserEntryCancelled))
            {
                DeviceInteractionInformation?.TrySetResult((new LinkDALRequestIPA5Object(), responseCode));
                return;
            }

            LinkDALRequestIPA5Object cardResponse = new LinkDALRequestIPA5Object();

            foreach (TLV tag in tags)
            {
                if (tag.Tag == E0Template.E0TemplateTag)
                {
                    foreach (TLV dataTag in tag.InnerTags)
                    {
                        if (dataTag.Tag == E0Template.KeyPress)
                        {
                            cardResponse.DALResponseData = new LinkDALActionResponse
                            {
                                Status = UserInteraction.UserKeyPressed.GetStringValue(),
                                Value = BCDConversion.StringFromByteData(dataTag.Data)
                            };
                            returnResponse = true;
                            break;
                        }
                    }

                    break;
                }
                else if (tag.Tag == E0Template.HTMLKeyPress)
                {
                    cardResponse.DALResponseData = new LinkDALActionResponse
                    {
                        Status = "UserKeyPressed",
                        Value = tag.Data[3] switch
                        {
                            // button actions as reported from HTML page
                            0x00 => DeviceKeys.KEY_2.ToString(),
                            0x1B => DeviceKeys.KEY_STOP.ToString(),
                            0x01 => DeviceKeys.KEY_1.ToString(),
                            0x0D => DeviceKeys.KEY_OK.ToString(),
                            _ => DeviceKeys.KEY_NONE.ToString()
                        }
                    };
                    returnResponse = true;
                    break;
                }
            }

            if (returnResponse)
            {
                DeviceInteractionInformation?.TrySetResult((cardResponse, responseCode));
            }
        }

        public void Get24HourRebootResponseHandler(List<TLV> tags, int responseCode, bool cancelled = false)
        {
            if (cancelled || tags == null)
            {
                Reboot24HourInformation?.TrySetResult((null, responseCode));
                return;
            }

            string deviceResponse = string.Empty;

            foreach (var tag in tags)
            {
                if (tag.Tag == E0Template.Reboot24HourTag)
                {
                    deviceResponse = Encoding.UTF8.GetString(tag.Data);
                    break;
                }
            }

            // command must always be processed
            Reboot24HourInformation?.TrySetResult((deviceResponse, responseCode));
        }

        public void GetTerminalDateTimeResponseHandler(byte[] data, int dataLength, int responseCode, bool cancelled = false)
        {
            if (cancelled)
            {
                TerminalDateTimeInformation?.TrySetResult((null, responseCode));
                return;
            }

            string deviceResponse = string.Empty;

            if (responseCode == (int)VipaSW1SW2Codes.Success && data?.Length > 0)
            {
                deviceResponse = Encoding.UTF8.GetString(data);
            }

            // command must always be processed
            TerminalDateTimeInformation?.TrySetResult((deviceResponse, responseCode));
        }

        public void ManualPanEntryStatusHandler(List<TLV> tags, int responseCode, bool cancelled = false)
        {
            if (cancelled)
            {
                int response = responseCode == (int)VipaSW1SW2Codes.Success ? (int)VipaSW1SW2Codes.UserEntryCancelled : responseCode;
                DecisionRequiredInformation?.TrySetResult((null, response));
                return;
            }

            var cardResponse = new LinkDALRequestIPA5Object();

            if ((responseCode == (int)VipaSW1SW2Codes.CommandCancelled) || (responseCode == (int)VipaSW1SW2Codes.UserEntryCancelled))
            {
                bool userCancelled = (responseCode == (int)VipaSW1SW2Codes.CommandCancelled) || (responseCode == (int)VipaSW1SW2Codes.UserEntryCancelled);
                // 9F41 - PIN Entry Timeout
                // 9F43 - Cardholder cancellation
                // Clear key <CORR> press needs to be ignored or it will end the selection: nothing was selected since tag DFA202 is not sent back.
                cardResponse.DALResponseData = new LinkDALActionResponse
                {
                    Status = UserInteraction.UserKeyPressed.GetStringValue(),
                    Value = Encoding.UTF8.GetString(new byte[] { 0x00, (byte)(userCancelled ? DeviceKeys.KEY_STOP : DeviceKeys.KEY_CORR) })
                };
                DecisionRequiredInformation?.TrySetResult((cardResponse, responseCode));
                return;
            }

            if (responseCode == (int)VipaSW1SW2Codes.Success && tags != null)
            {
                foreach (TLV tag in tags)
                {
                    if (tag.Tag == E0Template.E0TemplateTag)
                    {
                        E0TemplateManualPanProcessing(cardResponse, tag);
                        break;
                    }
                }
            }

            if (responseCode == (int)VipaSW1SW2Codes.Success && tags.Count > 0 && tags[0].InnerTags?.Count > 0)
            {
                DecisionRequiredInformation?.TrySetResult((cardResponse, responseCode));
            }
            else
            {
                // log error responses for device troubleshooting purposes
                DeviceLogger(LogLevel.Error, string.Format("VIPA STATUS CODE=0x{0:X4}", responseCode));
            }
        }

        #endregion --- response handlers ---

        public void LoadDeviceSectionConfig(DeviceSection config, Modes.Execution executionMode)
        {
            //preSwipeCardStorage.SetConfig(config?.Verifone?.PreSwipeTimeout ?? 0);
            DeviceInformation.ConfigurationHostId = config?.Verifone?.ConfigurationHostId ?? VerifoneSettingsSecurityConfiguration.ConfigurationHostId;
            DeviceInformation.OnlinePinKeySetId = config?.Verifone?.OnlinePinKeySetId ?? VerifoneSettingsSecurityConfiguration.OnlinePinKeySetId;
            DeviceInformation.ADEKeySetId = config?.Verifone?.ADEKeySetId ?? VerifoneSettingsSecurityConfiguration.ADEKeySetId;

            // program execution mode
            ExecutionMode = executionMode;
        }
    }
}
