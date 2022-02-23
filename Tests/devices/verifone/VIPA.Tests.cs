using Common.XO.Private;
using Devices.Common;
using Devices.Common.Config;
using Devices.Verifone.Helpers;
using Devices.Verifone.VIPA;
using Devices.Verifone.VIPA.Helpers;
using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using TestHelper;
using Xunit;

namespace Devices.Verifone.Tests
{
    public class VIPATests
    {
        const int ResponseHandlerDelay = 100;

        readonly VIPAImpl subject;

        public VIPATests()
        {
            subject = new VIPAImpl();
        }

        [Theory]
        [InlineData("sphere.sphere.idle...199.m400.1.210823", "1")]
        [InlineData("sphere.sphere.vipa....m400.6_8_2_17.210714", "6_8_2_17")]
        [InlineData("sphere.sphere.emv.unattended.FD...6_8_2_19.210816", "6_8_2_19")]
        public void ProcessVersionString_Maps_Correctly_To_Version_Schema(string schemaValue, string expectedVersion)
        {
            DALBundleVersioning bundle = new DALBundleVersioning();

            Helper.CallPrivateMethod<int>("ProcessVersionString", subject, out int result, new object[] { bundle, schemaValue });

            Assert.Equal((int)VipaSW1SW2Codes.Success, result);
            Assert.False(string.IsNullOrEmpty(bundle.Version));
            Assert.Equal(expectedVersion, bundle.Version);
        }

        [Theory]
        [InlineData("idle_ver.txt", "sphere.sphere.idle...199.m400.1.210823", "1")]
        [InlineData("vipa_ver.txt", "sphere.sphere.vipa....m400.6_8_2_17.210810", "6_8_2_17")]
        [InlineData("emv_ver.txt", "sphere.sphere.emv.unattended.FD...6_8_2_19.210816", "6_8_2_19")]
        public void ProcessVersionString_Maps_Correctly_To_Version_Schema_WhenLoaded_FromFile(string fileVersion, string schemaValue, string expectedVersion)
        {
            string fileContexts;
            string fileName = Environment.CurrentDirectory;
            int position = fileName.IndexOf("bin");
            if (position > 0)
            {
                fileName = Path.Combine(fileName.Substring(0, position), Path.Combine("Bundles", fileVersion));
            }

            using (var fileStream = new StreamReader(fileName, Encoding.UTF8))
            {
                fileContexts = fileStream.ReadToEnd();
            }

            Assert.Equal(schemaValue, fileContexts);

            DALBundleVersioning bundle = new DALBundleVersioning();

            Helper.CallPrivateMethod<int>("ProcessVersionString", subject, out int result, new object[] { bundle, fileContexts });

            Assert.Equal((int)VipaSW1SW2Codes.Success, result);
            Assert.False(string.IsNullOrEmpty(bundle.Version));
            Assert.Equal(expectedVersion, bundle.Version);
        }

        [Theory]
        [InlineData(BinaryStatusObject.DEVICE_M400, "sphere.sphere.vipa....m400.6_8_2_11.210714", "sphere.sphere.emv.attended.FD...6_2_8_11.210625", "sphere.sphere.idle...199.m400.1.210625", false)]
        [InlineData(BinaryStatusObject.DEVICE_P200, "sphere.sphere.vipa....p200.6_8_2_11.210714", "sphere.sphere.emv.attended.FD...6_2_8_11.210625", "sphere.sphere.idle...199.p200.1.210625", false)]
        [InlineData(BinaryStatusObject.DEVICE_P400, "sphere.sphere.vipa....p400.6_8_2_11.210714", "sphere.sphere.emv.attended.FD...6_2_8_11.210625", "sphere.sphere.idle...199.p400.1.210625", false)]
        [InlineData(BinaryStatusObject.DEVICE_UX301, "sphere.sphere.vipa....ux301.6_8_2_19.210714", "sphere.sphere.emv.unattended.FD...6_2_8_19.210625", "", false)]
        public void VIPAVersions_ReturnsProperlyFormatted_Schema(string deviceModel, string vipaVer, string emvVer, string idleVer, bool hmacEnabled)
        {
            DeviceInfoObject deviceInfoObject = new DeviceInfoObject();

            subject.DeviceIdentifier = new TaskCompletionSource<(DeviceInfoObject deviceInfoObject, int VipaResponse)>();
            subject.DeviceInformation = new DeviceInformation()
            {
                ConfigurationHostId = VerifoneSettingsSecurityConfiguration.ConfigurationHostId,
                ADEKeySetId = VerifoneSettingsSecurityConfiguration.ADEKeySetId,
                VipaPackageTag = vipaVer,
                CertPackageTag = emvVer,
                IdleImagePackageTag = idleVer
            };

            subject.DeviceInformation.FirmwareVersion = "6.8.2.11";
            subject.DeviceInformation.Model = deviceModel;

            // Device model/serial numbers and other info: ResetDevice
            subject.DeviceBinaryStatusInformation = new TaskCompletionSource<(BinaryStatusObject binaryStatusObject, int VipaResponse)>();
            BinaryStatusObject binaryStatusObject = new BinaryStatusObject();

            // delay a little before triggering the response handler
            Task.Run(async () =>
            {
                // P2PE VALIDATION: populate signed bundle package information
                await Task.Delay(ResponseHandlerDelay);

                // vipa_ver.txt
                int setStatus = (int)VipaSW1SW2Codes.Failure;
                if (!string.IsNullOrEmpty(vipaVer))
                {
                    setStatus = (int)VipaSW1SW2Codes.Success;
                    binaryStatusObject.FileSize = vipaVer.Length;
                    binaryStatusObject.ReadResponseBytes = ArrayPool<byte>.Shared.Rent(binaryStatusObject.FileSize);
                    Array.Copy(Encoding.ASCII.GetBytes(vipaVer), 0, binaryStatusObject.ReadResponseBytes, 0, binaryStatusObject.FileSize);
                }
                // GetBinaryStatus
                Debug.WriteLine("VIPA_VER: GetBinaryStatus - set");
                subject.DeviceBinaryStatusInformation.TrySetResult((binaryStatusObject, setStatus));
                if (setStatus == (int)VipaSW1SW2Codes.Success)
                {
                    // SelectFile
                    await Task.Delay(ResponseHandlerDelay);
                    Debug.WriteLine("VIPA_VER: SelectFile - set");
                    subject.DeviceBinaryStatusInformation.TrySetResult((binaryStatusObject, (int)VipaSW1SW2Codes.Success));
                    // ReadBinary
                    await Task.Delay(ResponseHandlerDelay);
                    Debug.WriteLine("VIPA_VER: ReadBinary - set");
                    subject.DeviceBinaryStatusInformation.TrySetResult((binaryStatusObject, (int)VipaSW1SW2Codes.Success));
                }

                // emv_ver.txt
                await Task.Delay(ResponseHandlerDelay);
                setStatus = (int)VipaSW1SW2Codes.Failure;
                if (!string.IsNullOrEmpty(emvVer))
                {
                    setStatus = (int)VipaSW1SW2Codes.Success;
                    binaryStatusObject.FileSize = emvVer.Length;
                    binaryStatusObject.ReadResponseBytes = ArrayPool<byte>.Shared.Rent(binaryStatusObject.FileSize);
                    Array.Copy(Encoding.ASCII.GetBytes(emvVer), 0, binaryStatusObject.ReadResponseBytes, 0, binaryStatusObject.FileSize);
                }
                // GetBinaryStatus
                await Task.Delay(ResponseHandlerDelay);
                Debug.WriteLine("EMV_VER: GetBinaryStatus - set");
                subject.DeviceBinaryStatusInformation.TrySetResult((binaryStatusObject, setStatus));
                if (setStatus == (int)VipaSW1SW2Codes.Success)
                {
                    // SelectFile
                    await Task.Delay(ResponseHandlerDelay);
                    Debug.WriteLine("EMV_VER: SelectFile - set");
                    subject.DeviceBinaryStatusInformation.TrySetResult((binaryStatusObject, (int)VipaSW1SW2Codes.Success));
                    // ReadBinary
                    await Task.Delay(ResponseHandlerDelay);
                    Debug.WriteLine("EMV_VER: ReadBinary - set");
                    subject.DeviceBinaryStatusInformation.TrySetResult((binaryStatusObject, (int)VipaSW1SW2Codes.Success));
                }

                // idle_ver.txt: 199
                await Task.Delay(ResponseHandlerDelay);
                setStatus = (int)VipaSW1SW2Codes.Failure;
                if (!string.IsNullOrEmpty(idleVer))
                {
                    setStatus = (int)VipaSW1SW2Codes.Success;
                    binaryStatusObject.FileSize = idleVer.Length;
                    binaryStatusObject.ReadResponseBytes = ArrayPool<byte>.Shared.Rent(binaryStatusObject.FileSize);
                    Array.Copy(Encoding.ASCII.GetBytes(idleVer), 0, binaryStatusObject.ReadResponseBytes, 0, binaryStatusObject.FileSize);
                }
                // GetBinaryStatus
                await Task.Delay(ResponseHandlerDelay);
                Debug.WriteLine("IDLE_VER: GetBinaryStatus - set");
                subject.DeviceBinaryStatusInformation.TrySetResult((binaryStatusObject, setStatus));
                if (setStatus == (int)VipaSW1SW2Codes.Success)
                {
                    // SelectFile
                    await Task.Delay(ResponseHandlerDelay);
                    Debug.WriteLine("IDLE_VER: SelectFile - set");
                    subject.DeviceBinaryStatusInformation.TrySetResult((binaryStatusObject, (int)VipaSW1SW2Codes.Success));
                    // ReadBinary
                    await Task.Delay(ResponseHandlerDelay);
                    Debug.WriteLine("IDLE_VER: ReadBinary - set");
                    subject.DeviceBinaryStatusInformation.TrySetResult((binaryStatusObject, (int)VipaSW1SW2Codes.Success));
                }
            });

            LinkDALRequestIPA5Object vipaVersions = subject.VIPAVersions(deviceModel, hmacEnabled, "199");

            Assert.NotNull(vipaVersions.DALCdbData);
            Assert.False(string.IsNullOrEmpty(vipaVersions.DALCdbData.EMVVersion.Version));
            Assert.False(string.IsNullOrEmpty(vipaVersions.DALCdbData.VIPAVersion.Version));
            // UX does not have an idle image
            if (string.IsNullOrEmpty(idleVer))
            {
                Assert.True(string.IsNullOrEmpty(vipaVersions.DALCdbData.IdleVersion.Version));
            }
            else
            {
                Assert.False(string.IsNullOrEmpty(vipaVersions.DALCdbData.IdleVersion.Version));
            }
        }

        [Fact]
        public void ProcessVersion_Maps_Correctly_WhenMutipleFilesPresent_InSourceDirectory()
        {
            string filepath = Environment.CurrentDirectory;
            int position = filepath.IndexOf("bin");
            if (position > 0)
            {
                filepath = Path.Combine(filepath.Substring(0, position), "Bundles");
            }

            DirectoryInfo d = new DirectoryInfo(filepath);
            foreach (FileInfo file in d.GetFiles("*.txt"))
            {
                string fileContexts;
                using (var fileStream = new StreamReader(file.FullName, Encoding.UTF8))
                {
                    fileContexts = fileStream.ReadToEnd();

                    DALBundleVersioning bundle = new DALBundleVersioning();

                    Helper.CallPrivateMethod<int>("ProcessVersionString", subject, out int result, new object[] { bundle, fileContexts });

                    Assert.Equal((int)VipaSW1SW2Codes.Success, result);
                    Assert.False(string.IsNullOrEmpty(bundle.Version));

                    Debug.WriteLine($"VIPA: {file.Name.ToUpper().PadRight(12)} - version=[{bundle.Version}]");
                }
            }
        }
    }
}
