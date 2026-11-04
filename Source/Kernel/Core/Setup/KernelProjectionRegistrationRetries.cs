// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Projections.Kernel;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Setup;

/// <summary>
/// Retries deferred registrations independently for each event store until success or silo shutdown.
/// </summary>
/// <param name="kernelProjections">The kernel projection registry.</param>
/// <param name="logger">The logger.</param>
/// <param name="timeProvider">The time provider for retry delays.</param>
/// <param name="nextJitter">A random value between zero and one.</param>
[Singleton]
public sealed class KernelProjectionRegistrationRetries(
    IKernelProjections kernelProjections,
    ILogger<KernelProjectionRegistrationRetries> logger,
    TimeProvider? timeProvider = null,
    Func<double>? nextJitter = null) : IKernelProjectionRegistrationRetries, IAsyncDisposable
{
    readonly ConcurrentDictionary<EventStoreName, Task> _pending = new();
    readonly ConcurrentDictionary<EventStoreName, int> _attempts = new();
    readonly CancellationTokenSource _stopping = new();
    readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    readonly Func<double> _nextJitter = nextJitter ?? Random.Shared.NextDouble;

    /// <inheritdoc/>
    public IReadOnlyCollection<EventStoreName> Pending => _pending.Keys.ToArray();

    /// <inheritdoc/>
    public int GetAttemptCount(EventStoreName eventStore) => _attempts.GetValueOrDefault(eventStore);

    /// <inheritdoc/>
    public void Defer(EventStoreName eventStore)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        if (_stopping.IsCancellationRequested || !_pending.TryAdd(eventStore, completion.Task)) return;

        // Publish the entry before starting: even an immediately completing registration must remove it.
        _ = Retry(eventStore, completion);
    }

    /// <inheritdoc/>
    public void Stop()
    {
        lock (_stopping)
        {
            if (!_stopping.IsCancellationRequested) _stopping.Cancel();
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        Stop();
        await Task.WhenAll(_pending.Values);
        lock (_stopping) _stopping.Dispose();
    }

    async Task Retry(EventStoreName eventStore, TaskCompletionSource completion)
    {
        var delay = TimeSpan.FromSeconds(5);
        try
        {
            while (!_stopping.IsCancellationRequested)
            {
                var jitteredDelay = delay * (0.5 + (_nextJitter() * 0.5));
                await Task.Delay(jitteredDelay, _timeProvider, _stopping.Token);
                _stopping.Token.ThrowIfCancellationRequested();
                _attempts.AddOrUpdate(eventStore, 1, (_, attempts) => attempts + 1);
                if (await TryRegister(eventStore))
                {
                    _attempts.TryRemove(eventStore, out _);
                    _pending.TryRemove(eventStore, out _);
                    logger.RegistrationSucceeded(eventStore);
                    return;
                }
                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 60));
            }
        }
        catch (OperationCanceledException) when (_stopping.IsCancellationRequested)
        {
            logger.RegistrationRetryStopped(eventStore);
        }
        finally
        {
            completion.TrySetResult();
        }
    }

    async Task<bool> TryRegister(EventStoreName eventStore)
    {
        try
        {
            // Orleans calls already have response timeouts. Do not overlap an abandoned attempt with the next one.
            await kernelProjections.DiscoverAndRegister(eventStore);
            return true;
        }
        catch (Exception exception)
        {
            if (SiblingSiloInstability.IsTransient(exception)) logger.TransientRegistrationFailure(eventStore, exception);
            else logger.RegistrationFailure(eventStore, exception);
            return false;
        }
    }
}
