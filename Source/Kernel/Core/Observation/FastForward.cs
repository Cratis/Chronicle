// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents the command for fast forwarding an observer to the tail of its event sequence.
/// </summary>
/// <param name="EventStore">The event store the observer belongs to.</param>
/// <param name="Namespace">The namespace within the event store the observer belongs to.</param>
/// <param name="ObserverId">The identifier of the observer.</param>
/// <param name="EventSequenceId">The event sequence the observer observes; uses its stored sequence when empty, or the event log if unavailable.</param>
/// <remarks>
/// All events the observer has not yet handled are skipped without invoking the observer. Use it to get past a
/// backlog, or events that can never be handled, when the read side does not need to be rebuilt.
/// </remarks>
[Command]
[Cratis.Arc.Authorization.Authorize(Policy = Security.WellKnownAuthorizationPolicies.AdministratorOperations)]
[Security.DestructiveOperation]
[BelongsTo(WellKnownServices.Observers)]
public record FastForward(string EventStore, string Namespace, string ObserverId, string EventSequenceId)
{
    /// <summary>
    /// Handles the command by accepting it as a job that fast forwards the target observer grain.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to get observer grains with.</param>
    /// <param name="storage">The storage containing observer definitions.</param>
    /// <returns>A <see cref="FastForwardResult"/> with the outcome <see cref="FastForwardOutcome.Accepted"/>; the job reports what became of it.</returns>
    /// <exception cref="ObserverOperatorCommandNotAccepted">The jobs manager did not accept the command.</exception>
    public async Task<FastForwardResult> Handle(IGrainFactory grainFactory, IStorage storage)
    {
        var eventSequenceId = await ObserverEventSequenceResolver.Resolve(storage, (EventStoreName)EventStore, (ObserverId)ObserverId, EventSequenceId);
        var key = new ObserverKey((ObserverId)ObserverId, (EventStoreName)EventStore, (EventStoreNamespaceName)Namespace, eventSequenceId);
        var jobId = await ObserverOperatorCommandJobs.Accept(grainFactory, key, ObserverOperatorCommand.FastForward);
        return jobId == JobId.NotSet
            ? throw new ObserverOperatorCommandNotAccepted(ObserverOperatorCommand.FastForward, key)
            : new FastForwardResult(FastForwardOutcome.Accepted, EventSequenceNumber.Unavailable, EventSequenceNumber.Unavailable);
    }
}
