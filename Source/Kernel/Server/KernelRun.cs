// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Server;

/// <summary>
/// Runs the kernel host and records how the run ended.
/// </summary>
/// <remarks>
/// This does what <see cref="HostingAbstractionsHostExtensions.RunAsync(IHost, CancellationToken)"/> does - start, wait for a stop request, stop, then
/// release the services - but one phase at a time, so each failure is attributed to the phase that threw it. The
/// host's lifetime tokens cannot tell this: the host signals that it stopped before it rethrows what failed while
/// stopping. A stop timeout is taken from the deadline actually elapsing, because a timed-out stop usually throws
/// nothing at all.
/// </remarks>
internal static class KernelRun
{
    /// <summary>
    /// Runs the host until it is asked to stop, then stops it and releases its services.
    /// </summary>
    /// <param name="host">The <see cref="IHost"/> to run.</param>
    /// <param name="stopRequest">A <see cref="CancellationToken"/> that requests the host to stop.</param>
    /// <returns>The <see cref="KernelRunResult"/>.</returns>
    /// <remarks>
    /// A failure to start is rethrown, after the host is released, and is not part of the result.
    /// </remarks>
    /// <exception cref="AggregateException">Starting failed, and releasing the host afterwards failed too.</exception>
    internal static async Task<KernelRunResult> RunAsync(IHost host, CancellationToken stopRequest)
    {
        // Resolved before running: releasing the host disposes the services these come from. The tokens stay
        // readable afterwards, since the lifetime does not dispose their sources and IsCancellationRequested never throws.
        var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
        var stopping = lifetime.ApplicationStopping;
        var shutdownTimeout = host.Services.GetRequiredService<IOptions<HostOptions>>().Value.ShutdownTimeout;

        var phase = KernelRunPhase.Starting;
        Exception? failure = null;
        var stopTimedOut = false;
        var stopDuration = TimeSpan.Zero;

        try
        {
            await host.StartAsync(stopRequest);
            await WaitForStopRequest(lifetime, stopRequest);

            phase = KernelRunPhase.Stopping;
            var stopwatch = Stopwatch.StartNew();
            using var deadline = new CancellationTokenSource(shutdownTimeout);
            try
            {
                await host.StopAsync(deadline.Token);
            }
            finally
            {
                stopDuration = stopwatch.Elapsed;
                stopTimedOut = deadline.IsCancellationRequested;
            }

            phase = KernelRunPhase.Disposing;
        }
        catch (Exception exception) when (phase != KernelRunPhase.Starting || KernelExit.IsStopDuringStartup(exception, StopRequested()))
        {
            failure = exception;
        }
        catch (Exception startFailure)
        {
            try
            {
                await Release(host);
            }
            catch (Exception releaseFailure)
            {
                throw new AggregateException(startFailure, releaseFailure);
            }

            throw;
        }

        try
        {
            await Release(host);
            if (phase == KernelRunPhase.Disposing)
            {
                phase = KernelRunPhase.Ended;
            }
        }
        catch (Exception exception) when (failure is not null && phase != KernelRunPhase.Starting)
        {
            // The stop already failed or timed out; that stays the outcome, and the release failure is kept with it.
            failure = new AggregateException(failure, exception);
        }
        catch (Exception exception)
        {
            phase = KernelRunPhase.Disposing;
            failure = exception;
        }

        var outcome = KernelExit.Classify(phase, failure, StopRequested(), stopTimedOut);
        return new(outcome, failure, shutdownTimeout, stopDuration);

        bool StopRequested() => stopRequest.IsCancellationRequested || stopping.IsCancellationRequested;
    }

    static async Task WaitForStopRequest(IHostApplicationLifetime lifetime, CancellationToken stopRequest)
    {
        var requested = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var forward = stopRequest.Register(lifetime.StopApplication);
        await using var signal = lifetime.ApplicationStopping.Register(() => requested.TrySetResult());
        await requested.Task;
    }

    static async Task Release(IHost host)
    {
        if (host is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
            return;
        }

        host.Dispose();
    }
}
