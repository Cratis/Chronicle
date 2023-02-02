// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.DependencyInversion;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Observation;
using Orleans;

namespace Aksio.Cratis.Kernel.Grains.Observation;

/// <summary>
/// Represents an implementation of <see cref="ICatchUp"/>.
/// </summary>
public class CatchUp : Observer<CatchUpState>, ICatchUp
{
    readonly IExecutionContextManager _executionContextManager;
    readonly ProviderFor<IEventSequenceStorageProvider> _eventSequenceStorageProvider;
    ObserverId? _observerId;
    ObserverKey? _observerKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="CatchUp"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for </param>
    /// <param name="eventSequenceStorageProvider">Provider for <see cref="IEventSequenceStorageProvider"/>.</param>
    public CatchUp(
        IExecutionContextManager executionContextManager,
        ProviderFor<IEventSequenceStorageProvider> eventSequenceStorageProvider)
    {
        _executionContextManager = executionContextManager;
        _eventSequenceStorageProvider = eventSequenceStorageProvider;
    }

    /// <inheritdoc/>
    public override Task OnActivateAsync()
    {
        _observerId = this.GetPrimaryKey(out var keyAsString);
        _observerKey = ObserverKey.Parse(keyAsString);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Start()
    {
        RegisterTimer(PerformCatchUp, null, TimeSpan.Zero, TimeSpan.MaxValue);
        return Task.CompletedTask;
    }

    async Task PerformCatchUp(object arg)
    {
        _executionContextManager.Establish(_observerKey!.TenantId, CorrelationId.New(), _observerKey!.MicroserviceId);
        var provider = _eventSequenceStorageProvider();

        var cursor = await provider.GetFromSequenceNumber(_observerKey!.EventSequenceId!, State.NextEventSequenceNumber, eventTypes: State.EventTypes);
        while (await cursor.MoveNext())
        {
            foreach (var @event in cursor.Current)
            {
                if (await Handle(@event))
                {
                    State.NextEventSequenceNumber = @event.Metadata.SequenceNumber + 1;
                    await WriteStateAsync();
                }
            }
        }

        await GrainFactory.GetGrain<IObserverSupervisor>(_observerId!, _observerKey!).NotifyCatchUpComplete();
    }
}
