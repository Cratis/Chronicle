// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Reactors.Kernel;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Alerts;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Alerts;

/// <summary>
/// Materializes recorded alert transitions in the System store Default namespace.
/// </summary>
/// <param name="storage">The storage registry.</param>
/// <param name="logger">The reactor logger.</param>
[Reactor(id: Id, eventSequence: WellKnownEventSequences.System, systemEventStoreOnly: true, defaultNamespaceOnly: true)]
public class AlertIncidentsReactor(IStorage storage, ILogger<AlertIncidentsReactor> logger) : Reactor
{
    /// <summary>
    /// The discovery identity before the kernel-owned prefix is applied.
    /// </summary>
    public const string Id = "Cratis.Chronicle.Alerts.Incidents";

    /// <summary>
    /// The exact registered observer key used for health sampling.
    /// </summary>
    public static readonly ObserverKey ObserverKey = new($"$system.{Id}", EventStoreName.System, EventStoreNamespaceName.Default, EventSequenceId.System);

    /// <summary>
    /// Handles a recorded raise.
    /// </summary>
    /// <param name="event">Recorded event.</param>
    /// <param name="context">Persisted context.</param>
    /// <returns>Awaitable persistence.</returns>
    public Task Raised(AlertRaised @event, EventContext context) => Apply(@event.ToTransition(context));

    /// <summary>
    /// Handles a recorded escalation.
    /// </summary>
    /// <param name="event">Recorded event.</param>
    /// <param name="context">Persisted context.</param>
    /// <returns>Awaitable persistence.</returns>
    public Task Escalated(AlertEscalated @event, EventContext context) => Apply(@event.ToTransition(context));

    /// <summary>
    /// Handles a recorded clear.
    /// </summary>
    /// <param name="event">Recorded event.</param>
    /// <param name="context">Persisted context.</param>
    /// <returns>Awaitable persistence.</returns>
    public Task Cleared(AlertCleared @event, EventContext context) => Apply(@event.ToTransition(context));

    async Task Apply(AlertIncidentTransition transition)
    {
        var outcome = await storage.GetEventStore(EventStoreName.System).GetNamespace(EventStoreNamespaceName.Default)
            .AlertIncidents.Apply(transition);
        if (outcome == AlertIncidentWriteOutcome.OrphanEscalation)
        {
            logger.OrphanEscalation(transition.Id, transition.SequenceNumber);
        }
    }
}
