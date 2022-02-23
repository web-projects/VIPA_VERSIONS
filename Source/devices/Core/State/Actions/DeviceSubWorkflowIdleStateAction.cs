using Devices.Core.State.Providers;
using Devices.Core.State.SubWorkflows.Management;
using System.Threading.Tasks;
using static Devices.Core.State.Enums.DeviceWorkflowState;

namespace Devices.Core.State.Actions
{
    internal class DeviceSubWorkflowIdleStateAction : DeviceBaseStateAction
    {
        public override Enums.DeviceWorkflowState WorkflowStateType => SubWorkflowIdleState;

        public DeviceSubWorkflowIdleStateAction(Interfaces.IDeviceStateController _) : base(_) { }

        private Devices.Core.State.SubWorkflows.Management.IDeviceSubStateManager currentManager;

        public override Task DoWork()
        {
            IControllerVisitorProvider visitorProvider = Controller.GetCurrentVisitorProvider();
            ISubStateManagerProvider subStateProvider = Controller.GetSubStateManagerProvider();

            var visitor = visitorProvider.CreateBoundarySetupVisitor();

            IDeviceSubStateManager subStateManager = subStateProvider.GetSubStateManager(Controller);
            subStateManager.Accept(visitor);

            if (StateObject is null)
            {
                _ = Complete(this);
            }
            else
            {
                currentManager = subStateManager;

                subStateManager.SubWorkflowComplete += OnSubWorkflowCompleted;
                subStateManager.SubWorkflowError += OnSubWorkflowErrored;

                SubWorkflows.WorkflowOptions launchOptions = StateObject as SubWorkflows.WorkflowOptions;
                //launchOptions.ExecutionTimeout = launchOptions.StateObject.Actions?[0]?.PaymentRequest?.CardWorkflowControls?.CardCaptureTimeout * 1024;
                launchOptions.ExecutionTimeout = Helpers.DeviceConstants.CardCaptureTimeout * 1024;
                subStateManager.LaunchWorkflow(launchOptions);
            }

            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            if (currentManager != null)
            {
                currentManager.SubWorkflowComplete -= OnSubWorkflowCompleted;
                currentManager.SubWorkflowError -= OnSubWorkflowErrored;
                currentManager.Dispose();
                currentManager = null;
            }
        }

        private void TeardownVisitor()
        {
            if (currentManager != null)
            {
                Devices.Core.State.Providers.IControllerVisitorProvider visitorProvider = Controller.GetCurrentVisitorProvider();
                var visitor = visitorProvider.CreateBoundaryTeardownVisitor();

                currentManager.Accept(visitor);
            }
        }

        protected virtual void OnSubWorkflowCompleted()
        {
            TeardownVisitor();
            _ = Complete(this);
        }

        protected virtual void OnSubWorkflowErrored()
        {
            TeardownVisitor();
            _ = Error(this);
        }
    }
}
