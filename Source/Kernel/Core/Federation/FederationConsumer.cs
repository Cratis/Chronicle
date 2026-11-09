// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Thin Orleans orchestration around a <see cref="FederationLinkPump"/>: a grain timer drives the pump, and re-arms itself with the delay the pump asks for.
/// The keeper on every silo calls <see cref="IFederationConsumer.Ensure"/> periodically, which restarts a consumer after a silo died or after it stopped itself.
/// </summary>
/// <param name="registry">The federation registry.</param>
/// <param name="status">The federation status.</param>
/// <param name="clients">The factory of producer connections.</param>
/// <param name="inbox">The local inbox.</param>
/// <param name="subscriptions">The local subscriptions.</param>
/// <param name="metrics">The metrics.</param>
/// <param name="options">The Chronicle options.</param>
/// <param name="logger">The logger.</param>
/// <param name="timeProvider">The time provider.</param>
[KeepAlive]
public sealed class FederationConsumer(
    IFederationRegistry registry,
    IFederationStatus status,
    IFederationPullClientFactory clients,
    IFederationInbox inbox,
    IFederationSubscriptions subscriptions,
    IFederationConsumerMetrics metrics,
    IOptions<ChronicleOptions> options,
    ILogger<FederationLinkPump> logger,
    TimeProvider? timeProvider = null) : Grain, IFederationConsumer, IDisposable
{
    readonly CancellationTokenSource _cancellation = new();
    FederationLinkPump? _pump;
    IGrainTimer? _timer;
    bool _active;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _pump = new FederationLinkPump(
            this.GetPrimaryKeyString(),
            registry,
            status,
            clients,
            inbox,
            subscriptions,
            metrics,
            options,
            logger,
            timeProvider: timeProvider);

        _active = true;
        _timer = this.RegisterGrainTimer(
            Tick,
            new GrainTimerCreationOptions { DueTime = TimeSpan.Zero, Period = Timeout.InfiniteTimeSpan, KeepAlive = true, Interleave = false });
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        await _cancellation.CancelAsync();
        Dispose();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _timer?.Dispose();
        _pump?.Dispose();
        _cancellation.Dispose();
    }

    /// <inheritdoc/>
    public Task Ensure()
    {
        if (!_active)
        {
            _active = true;
            _timer?.Change(TimeSpan.Zero, Timeout.InfiniteTimeSpan);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Stop()
    {
        _active = false;
        _timer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        DeactivateOnIdle();
        return Task.CompletedTask;
    }

    async Task Tick(CancellationToken cancellationToken)
    {
        if (!_active || _pump is null)
        {
            return;
        }

        try
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellation.Token);
            var delay = await _pump.Step(linked.Token);
            if (delay is null)
            {
                _active = false;
                DeactivateOnIdle();
                return;
            }

            _timer?.Change(delay.Value, Timeout.InfiniteTimeSpan);
        }
        catch (OperationCanceledException)
        {
            // The grain is deactivating.
        }
    }
}
