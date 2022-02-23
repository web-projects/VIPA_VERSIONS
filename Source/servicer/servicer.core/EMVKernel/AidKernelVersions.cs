namespace Servicer.Core.EMVKernel
{
    public sealed class AidKernelVersions
    {
        public string AidValue { get; }
        public string KernelVersion { get; }
        public AidKernelVersions(string aidValue, string kernelVersion) => (AidValue, KernelVersion) = (aidValue, kernelVersion);
    }
}
