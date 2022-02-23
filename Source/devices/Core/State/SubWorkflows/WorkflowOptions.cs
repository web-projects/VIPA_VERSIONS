using Common.XO.Requests;

namespace Devices.Core.State.SubWorkflows
{
    internal class WorkflowOptions
    {
        public int? ExecutionTimeout;
        public LinkRequest StateObject;
    }
}
