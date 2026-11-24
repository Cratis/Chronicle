// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Diagnostics;
using Cratis.Chronicle.Observation.ObserverRouting;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Observation.Supervisors;

/// <summary>
/// Represents an implementation of <see cref="IObserverSupervisor"/>.
/// </summary>
/// <param name="stateProvider">The <see cref="IObserverDefinitionsStateProvider"/> the stored revision is read through.</param>
/// <param name="routingClient">The <see cref="IObserverRoutingClient"/> that publishes a revision to every silo.</param>
/// <param name="observers">The <see cref="IObservers"/> routing table of the silo.</param>
/// <param name="behindCheck">The <see cref="IObserverBehindCheck"/> that compares cursors with tails.</param>
/// <param name="storage">The <see cref="IStorage"/> the namespaces of the event store are read from.</param>
[KeepAlive]
public class ObserverSupervisor(
    IObserverDefinitionsStateProvider stateProvider,
    IObserverRoutingClient routingClient,
    IObservers observers,
    IObserverBehindCheck behindCheck,
    IStorage storage) : Grain, IObserverSupervisor
{
    ObserverSupervisorKey _key = new(EventStoreName.NotSet, ObserverId.Unspecified);

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _key = ObserverSupervisorKey.Parse(this.GetPrimaryKeyString());
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<ObserverRevisionAcknowledgements> Publish()
    {
        var revision = await stateProvider.GetStoredRevision(_key.EventStore, _key.ObserverId);
        return await routingClient.Publish(_key.EventStore, _key.ObserverId, revision);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ObserverKey>> FindBehind()
    {
        var table = await observers.GetTable(_key.EventStore);
        if (!table.TryGetRoute(_key.ObserverId, out var route))
        {
            return [];
        }

        var namespaces = await storage.GetEventStore(_key.EventStore).Namespaces.GetAll();
        var found = await Task.WhenAll(namespaces.Select(_ => FindBehindIn(route, _.Name)));
        return found.SelectMany(_ => _).ToArray();
    }

    async Task<IEnumerable<ObserverKey>> FindBehindIn(ObserverRoute route, EventStoreNamespaceName @namespace)
    {
        var observerKey = new ObserverKey(route.ObserverId, _key.EventStore, @namespace, route.EventSequenceId);
        try
        {
            var result = await behindCheck.Check(
                new EventSequenceKey(route.EventSequenceId, _key.EventStore, @namespace),
                [new ObserverBehindCheckCandidate(observerKey, route.EventTypes, route.AllEventTypes)]);
            return result.ToWake;
        }
        catch (Exception exception) when (ObservationOnlyFailures.CanBeContained(exception))
        {
            // A namespace that cannot be compared errs toward delivery.
            return [observerKey];
        }
    }
}
