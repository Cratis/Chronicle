// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences.Copy;
using Cratis.Chronicle.Grpc;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Starts a durable job that copies the events of one event source to another in the same event sequence.
/// </summary>
/// <param name="EventStore">The event store.</param>
/// <param name="Namespace">The namespace.</param>
/// <param name="EventSequenceId">The event sequence holding both event sources.</param>
/// <param name="SourceId">The event source to copy events from.</param>
/// <param name="TargetId">The event source to append the copies to.</param>
/// <param name="EventTypes">The identifiers of the event types to copy. When empty, every event type is copied.</param>
[Command]
[BelongsTo(WellKnownServices.EventSequences)]
public record CopyEventsForEventSource(
    EventStoreName EventStore,
    EventStoreNamespaceName Namespace,
    Concepts.EventSequences.EventSequenceId EventSequenceId,
    EventSourceId SourceId,
    EventSourceId TargetId,
    IEnumerable<string> EventTypes)
{
    /// <summary>
    /// Handles the command by starting the copy job.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to reach the jobs manager with.</param>
    /// <returns>The identifier of the started job, for following its progress through the jobs service.</returns>
    /// <exception cref="CouldNotStartCopyEvents">Thrown when the job could not be started.</exception>
    public async Task<Guid> Handle(IGrainFactory grainFactory)
    {
        var request = new CopyEventSourceEventsRequest(
            EventSequenceId,
            SourceId,
            TargetId,
            [.. (EventTypes ?? []).Select(_ => new EventTypeId(_))]);

        var result = await grainFactory.GetJobsManager(EventStore, Namespace)
            .Start<ICopyEventSourceEvents, CopyEventSourceEventsRequest>(request);

        if (result is null || !result.TryGetResult(out var jobId))
        {
            throw new CouldNotStartCopyEvents(SourceId, TargetId);
        }

        return jobId.Value;
    }
}
