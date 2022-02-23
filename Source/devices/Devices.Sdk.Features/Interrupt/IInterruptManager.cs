using System;
using System.Threading;
using System.Threading.Tasks;
using static Devices.Sdk.Features.InterruptFeatureOptions;

namespace Devices.Sdk.Features.Interrupt
{
    public interface IInterruptManager : IAsyncDisposable
    {
        string CurrentlyExecutedFeature { get; }
        int InterruptJobSize { get; }
        Task ExecuteAsync(IDeviceInterruptFeature feature, InterruptFeatureOptions options);
        Task StopAsync();
        Task StopAllAsync();
        CancellationToken GetShortLivedToken(int shortLivedDurationMS = InterruptFeatureNoTimeout);
        void NotifyStart(IDeviceInterruptFeature feature);
        void NotifyTick(IDeviceInterruptFeature feature);
        void NotifyCompletion(IDeviceInterruptFeature feature);
    }
}
