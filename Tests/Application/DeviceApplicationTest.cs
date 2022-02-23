using DEVICE_CORE.Modules;
using Moq;
using Ninject;
using Devices.Core.State.Management;
using System;
using Xunit;
using static Common.Execution.Modes;

namespace DEVICE_CORE.Application.Tests
{
    public class DeviceApplicationTest : IDisposable
    {
        readonly DeviceApplication subject;
        readonly Mock<IDeviceStateManager> mockStateManager;
        readonly DeviceStateMachineAsyncManager asyncManager;

        const string someFakePath = @"C:\fakepluginpath";

        public DeviceApplicationTest()
        {
            subject = new DeviceApplication();

            mockStateManager = new Mock<IDeviceStateManager>();

            using (IKernel kernel = new DeviceKernelResolver().ResolveKernel())
            {
                kernel.Rebind<IDeviceStateManager>().ToConstant(mockStateManager.Object);
                kernel.Inject(subject);
            }

            asyncManager = new DeviceStateMachineAsyncManager();
        }

        public void Dispose() => asyncManager.Dispose();

        [Fact]
        public void Initialize_ShouldSetPluginPath_When_Called()
        {
            subject.Initialize(someFakePath);

            string pluginPath = TestHelper.Helper.GetFieldValueFromInstance<string>("pluginPath", false, false, subject);

            Assert.Equal(someFakePath, pluginPath);
        }

        [Fact]
        public async void Run_ShouldSetPluginPath_And_ExecuteWorkflow_When_Called()
        {
            mockStateManager.Setup(e => e.LaunchWorkflow()).Callback(() => asyncManager.Trigger());

            subject.Initialize(someFakePath);
            await subject.Run(new Execution.AppExecConfig{ });

            Assert.True(asyncManager.WaitFor());

            mockStateManager.Verify(e => e.SetPluginPath(someFakePath), Times.Once());
            mockStateManager.Verify(e => e.LaunchWorkflow(), Times.Once());
        }

        [Fact]
        public void Shutdown_ShouldStopTheWorkflow_When_Called()
        {
            subject.Shutdown();

            mockStateManager.Verify(e => e.StopWorkflow(), Times.Once());

            Assert.Null(subject.DeviceStateManager);
        }

        [Fact]
        public void Shutdown_ShouldDoNothing_When_ShutdownHasAlreadyHappened()
        {
            subject.DeviceStateManager = null;

            subject.Shutdown();

            mockStateManager.Verify(e => e.StopWorkflow(), Times.Never());
        }
    }
}
