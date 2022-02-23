using Polly;
using Polly.Timeout;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Devices.Core.Cancellation
{
    internal class DeviceCancellationBrokerImpl : IDeviceCancellationBroker
    {
        /// <summary>
        /// Enforces that a cancellation token will be honored in IO Bound function
        /// pointer callbacks. This is an EXTREMELY dangerous class and should only be
        /// leveraged by callers that have a way to stop the executed function pointer.
        /// Although cancellation is honored, the synchronous method executed on an occupied
        /// thread pool continues execution so please use this class with extreme care.
        /// </summary>
        /// <typeparam name="TOutput"></typeparam>
        private class CancellationEnforcer<TOutput>
        {
            private bool valueSet;
            private TOutput result = default;

            public async Task<TOutput> AskVinnyToKeepWatch(Func<CancellationToken, TOutput> method, CancellationToken token)
            {
                try
                {
                    Task executionTask = Task.Factory.StartNew(() => {
                        result = method(token);
                        valueSet = true;
                    }, token);

                    while (true)
                    {
                        if (token.IsCancellationRequested)
                        {
                            token.ThrowIfCancellationRequested();
                            break;
                        }
                        else if (valueSet)
                        {
                            break;
                        }
                        await Task.Delay(10);
                    }
                    return result;
                }
                catch
                {
                    throw;
                }
            }
        }

        public async Task<PolicyResult<TOutput>> ExecuteWithTimeoutAsync<TOutput>(Func<CancellationToken, TOutput> func, int timeout, CancellationToken relatedToken)
        {
            CancellationEnforcer<TOutput> cancellationEnforcer = new CancellationEnforcer<TOutput>();

            // need a little bit of extra time to allow the globalTimer to execute first
            AsyncTimeoutPolicy timeoutPolicy = Policy.TimeoutAsync(timeout + 1, TimeoutStrategy.Optimistic);

            PolicyResult<TOutput> policyResult = await timeoutPolicy.ExecuteAndCaptureAsync(
                async ct => await cancellationEnforcer.AskVinnyToKeepWatch(func, ct), relatedToken
            );

            return policyResult;
        }
    }
}
