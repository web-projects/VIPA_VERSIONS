using Devices.Sdk.Features.State;
using Ninject;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using static Devices.Sdk.Features.InterruptFeatureOptions;

namespace Devices.Sdk.Features.Interrupt
{
    internal sealed class InterruptManagerImpl : IInterruptManager
    {
        private readonly ConcurrentStack<IDeviceInterruptFeature> interruptStack = new ConcurrentStack<IDeviceInterruptFeature>();
        private CancellationTokenSource tokenSource;
        private bool disposed;

        public string CurrentlyExecutedFeature { get; private set; }

        public int InterruptJobSize { get => interruptStack.Count; }

        [Inject]
        IInterruptFeatureValidator FeatureValidator { get; set; }

        public async ValueTask DisposeAsync()
        {
            if (!disposed)
            {
                disposed = true;

                ForceCancellationIfNecessary();

                while (InterruptJobSize > 0)
                {
                    if (interruptStack.TryPop(out IDeviceInterruptFeature feature))
                    {
                        await feature.InterruptAsync();
                        feature.Dispose();
                    }
                }
            }
        }

        public async Task ExecuteAsync(IDeviceInterruptFeature feature, InterruptFeatureOptions options)
        {
            // Validate that the current interrupt feature can be utilized for the current target device.
            if (!FeatureValidator.CanInterruptFeatureHandle(feature, options?.TargetDevice))
            {
                throw new InterruptManagementException($"The Interrupt Feature '{feature?.Name ?? "N/A"}' is not supported on your current payment device.");
            }

            // Set interrupt manger
            feature.SetInterruptManager(this);

            // Push the latest feature on to the top of the stack and start its execution
            // after validation takes place.
            feature.Validate(options);

            // Stop the currently executed interrupt feature and do not allow continutation
            // to occur because we are going to give this new feature priority over existing
            // stack items.
            await InterruptCurrentExecutionAsync();

            // We are now in a position to have the new interrupt feature take over at the top of the stack.
            // Keep in mind that although the function is executing it will not block for very long as it falls
            // into a different code path altogether while it begins its Tick functionality.
            interruptStack.Push(feature);

            await feature.ExecuteAsync(options);
        }

        public async Task StopAsync()
        {
            await StopWithContinuationPossibilityAsync();
        }

        public async Task StopAllAsync()
        {
            ForceCancellationIfNecessary();

            while (InterruptJobSize > 0)
            {
                if (interruptStack.TryPop(out IDeviceInterruptFeature feature))
                {
                    await feature.InterruptAsync();
                }
            }
        }

        public CancellationToken GetShortLivedToken(int shortLivedDurationMS = InterruptFeatureNoTimeout)
        {
            if (tokenSource != null)
            {
                tokenSource.Dispose();
                tokenSource = null;
            }

            tokenSource = shortLivedDurationMS switch
            {
                InterruptFeatureNoTimeout => new CancellationTokenSource(),
                _ => new CancellationTokenSource(shortLivedDurationMS)
            };

            return tokenSource.Token;
        }

        public void NotifyStart(IDeviceInterruptFeature feature)
        {
            CurrentlyExecutedFeature = feature.Name;
        }

        public void NotifyCompletion(IDeviceInterruptFeature feature)
        {
            RemoveTopOfStackFeature();

            Task.Run(async () => await ContinueNextExecution());
        }

        public void NotifyTick(IDeviceInterruptFeature feature)
        {
            // At this point the current interrupt feature has completed one cycle
            // and will not loop any further. For that reason, we must continue cycling it
            // so that it properly iterates until we're told to stop the current action.
            Task.Run(async () => await feature.ContinueAsync());
        }

        private Task ContinueNextExecution()
        {
            if (InterruptJobSize == 0)
                return Task.CompletedTask;

            if (!interruptStack.TryPeek(out IDeviceInterruptFeature topOfStackFeature))
            {
                throw new InterruptManagementException("There was an exception while attempting to continue feature at the top of the stack.");
            }

            Task.Run(async () => await topOfStackFeature.ContinueAsync());

            return Task.CompletedTask;
        }

        private void ForceCancellationIfNecessary()
        {
            if (tokenSource != null)
            {
                tokenSource.Cancel(true);
                tokenSource.Dispose();
                tokenSource = null;
            }
        }

        private void RemoveTopOfStackFeature()
        {
            CurrentlyExecutedFeature = DalFeatureList.DALFeatureDefault;

            if (!interruptStack.TryPop(out IDeviceInterruptFeature _))
            {
                throw new InterruptManagementException($"Unable to remove last interrupt feature off of the execution stack.");
            }
        }

        private async Task InterruptCurrentExecutionAsync()
        {
            ForceCancellationIfNecessary();

            if (InterruptJobSize > 0)
            {
                if (interruptStack.TryPeek(out IDeviceInterruptFeature feature))
                {
                    await feature.InterruptAsync();
                }
            }
        }

        private async Task StopWithContinuationPossibilityAsync(bool continuation = true)
        {
            ForceCancellationIfNecessary();

            if (InterruptJobSize > 0)
            {
                if (interruptStack.TryPop(out IDeviceInterruptFeature feature))
                {
                    await feature.InterruptAsync();
                }
            }

            if (continuation)
            {
                _ = ContinueNextExecution();
            }
        }
    }
}
