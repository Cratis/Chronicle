// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Makes sure a <see cref="IFederationConsumer"/> is running for every binding, from every silo: it polls the registry, activates consumers for
/// new and surviving bindings (which also re-activates them after a silo died), and stops the consumers of removed ones.
/// </summary>
/// <param name="grainFactory">The grain factory.</param>
/// <param name="logger">The logger.</param>
/// <param name="options">The Chronicle options.</param>
/// <param name="registry">The federation registry; absent until the registry is registered, in which case nothing runs.</param>
/// <param name="timeProvider">The time provider.</param>
public sealed class FederationConsumerKeeper(
    IGrainFactory grainFactory,
    ILogger<FederationConsumerKeeper> logger,
    IOptions<ChronicleOptions> options,
    IFederationRegistry? registry = null,
    TimeProvider? timeProvider = null) : ILifecycleParticipant<ISiloLifecycle>, IDisposable
{
    /// <summary>
    /// The time between registry polls.
    /// </summary>
    public static readonly TimeSpan Period = TimeSpan.FromSeconds(15);

    readonly HashSet<string> _known = new(StringComparer.Ordinal);
    CancellationTokenSource? _cancellation;
    Task _loop = Task.CompletedTask;

    /// <inheritdoc/>
    public void Dispose() => _cancellation?.Dispose();

    /// <inheritdoc/>
    public void Participate(ISiloLifecycle lifecycle) =>
        lifecycle.Subscribe(nameof(FederationConsumerKeeper), ServiceLifecycleStage.Active, Start, Stop);

    /// <summary>
    /// Reconcile the running consumers with the bindings once. Failures are logged and swallowed; the next poll tries again.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    public async Task Reconcile()
    {
        if (registry is null || !options.Value.Federation.Enabled)
        {
            return;
        }

        try
        {
            var bindings = await registry.GetBindings();
            var current = bindings.Select(_ => _.Id).ToHashSet(StringComparer.Ordinal);

            await Task.WhenAll(current.Select(Ensure));
            await Task.WhenAll(_known.Where(_ => !current.Contains(_)).ToArray().Select(StopRemoved));

            _known.Clear();
            _known.UnionWith(current);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.ReconcileFailed(exception);
        }
    }

    async Task Ensure(string id)
    {
        try
        {
            await grainFactory.GetGrain<IFederationConsumer>(id).Ensure();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // One binding that cannot be reached must not keep the others from starting.
            logger.EnsureFailed(id, exception);
        }
    }

    async Task StopRemoved(string id)
    {
        try
        {
            await grainFactory.GetGrain<IFederationConsumer>(id).Stop();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.EnsureFailed(id, exception);
        }
    }

    Task Start(CancellationToken cancellationToken)
    {
        _cancellation = new();
        _loop = DetachedExecution.Run(() => Loop(_cancellation.Token));
        return Task.CompletedTask;
    }

    async Task Stop(CancellationToken cancellationToken)
    {
        if (_cancellation is null)
        {
            return;
        }

        await _cancellation.CancelAsync();
        try
        {
            await _loop.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
    }

    async Task Loop(CancellationToken cancellationToken)
    {
        try
        {
            await Reconcile();
            using var timer = new PeriodicTimer(Period, timeProvider ?? TimeProvider.System);
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await Reconcile();
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
