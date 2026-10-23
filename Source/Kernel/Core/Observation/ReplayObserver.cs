// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents the command for replaying an observer from the beginning of its event sequence.
/// </summary>
/// <param name="EventStore">The name of the event store.</param>
/// <param name="Namespace">The namespace within the event store.</param>
/// <param name="ObserverId">The unique identifier of the observer to replay.</param>
/// <param name="EventSequenceId">The identifier of the event sequence; uses the stored observer sequence when empty, or the event log if unavailable.</param>
[Command]
[BelongsTo(WellKnownServices.Observers)]
public record ReplayObserver(string EventStore, string Namespace, string ObserverId, string EventSequenceId)
{
    internal const string ReplayNotStartedMessage = "Replay could not be started for this observer.";

    /// <summary>
    /// Handles the command by invoking <see cref="IObserver.Replay"/> on the target observer grain.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to get observer grains with.</param>
    /// <param name="storage">The storage containing observer definitions.</param>
    /// <returns>A validation error if no replay job was started; otherwise no validation error.</returns>
    public async Task<ValidationResult?> Handle(IGrainFactory grainFactory, IStorage storage)
    {
        var eventSequenceId = await ObserverEventSequenceResolver.Resolve(storage, (EventStoreName)EventStore, (ObserverId)ObserverId, EventSequenceId);
        var key = new ObserverKey((ObserverId)ObserverId, (EventStoreName)EventStore, (EventStoreNamespaceName)Namespace, eventSequenceId);
        var jobId = await grainFactory.GetGrain<IObserver>(key).Replay();
        return jobId == JobId.NotSet
            ? ValidationResult.Error(ReplayNotStartedMessage, [nameof(ObserverId)])
            : null;
    }
}
