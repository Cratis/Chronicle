// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.DependencyInversion;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Observation;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;

namespace Aksio.Cratis.Kernel.Grains.Observation;

/// <summary>
/// Represents an implementation of <see cref="ICatchUp"/>.
/// </summary>
public class CatchUp : Observer, ICatchUp
{
    ObserverKey? _observerKey;
    IDisposable? _timer;

    /// <summary>
    /// Initializes a new instance of the <see cref="CatchUp"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for.</param>
    /// <param name="eventSequenceStorageProvider">Provider for <see cref="IEventSequenceStorageProvider"/>.</param>
    /// <param name="observerState"><see cref="IPersistentState{T}"/> for the <see cref="ObserverState"/>.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public CatchUp(
        IExecutionContextManager executionContextManager,
        ProviderFor<IEventSequenceStorageProvider> eventSequenceStorageProvider,
        [PersistentState(nameof(ObserverState), Observation.ObserverState.CatchUpStorageProvider)] IPersistentState<ObserverState> observerState,
        ILogger<CatchUp> logger) : base(executionContextManager, eventSequenceStorageProvider, observerState, logger)
    {
    }

    /// <inheritdoc/>
    protected override MicroserviceId MicroserviceId => _observerKey!.MicroserviceId;

    /// <inheritdoc/>
    protected override TenantId TenantId => _observerKey!.TenantId;

    /// <inheritdoc/>
    protected override EventSequenceId EventSequenceId => _observerKey!.EventSequenceId;

    /// <inheritdoc/>
    protected override MicroserviceId? SourceMicroserviceId => _observerKey!.SourceMicroserviceId;

    /// <inheritdoc/>
    protected override TenantId? SourceTenantId => _observerKey!.SourceTenantId;

    /// <inheritdoc/>
    public override Task OnActivateAsync()
    {
        _ = this.GetPrimaryKey(out var keyAsString);
        _observerKey = ObserverKey.Parse(keyAsString);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Start(Type subscriberType)
    {
        SubscriberType = subscriberType;
        _timer = RegisterTimer(PerformCatchUp, null, TimeSpan.Zero, TimeSpan.MaxValue);
        return Task.CompletedTask;
    }

    async Task PerformCatchUp(object arg)
    {
        _timer?.Dispose();
        var provider = EventSequenceStorageProvider;

        var cursor = await provider.GetFromSequenceNumber(_observerKey!.EventSequenceId!, ObserverState.State.NextEventSequenceNumber, eventTypes: ObserverState.State.EventTypes);
        while (await cursor.MoveNext())
        {
            foreach (var @event in cursor.Current)
            {
                await Handle(@event, false);
            }
        }

        await Supervisor.NotifyCatchUpComplete();
    }
}
