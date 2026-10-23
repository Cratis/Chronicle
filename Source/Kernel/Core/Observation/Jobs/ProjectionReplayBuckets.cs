// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Plans opt-in buckets only when both the definition and the active engine subscription allow independent writes.
/// </summary>
internal static class ProjectionReplayBuckets
{
    internal static async Task<int> CountFor(ReplayObserverRequest request, IObserver observer, ChronicleOptions? options, IStorage storage)
    {
        if (!Enabled(options))
        {
            return 0;
        }

        var subscription = await observer.GetSubscription();
        if (!subscription.IsSubscribed || subscription.IsCollapsingProjection) return 0;
        var definitions = storage.GetEventStore(request.ObserverKey.EventStore).Projections;
        var id = new ProjectionId(request.ObserverKey.ObserverId.Value);
        if (!await definitions.Has(id)) return 0;
        var definition = await definitions.Get(id);
        return PartitionReplayabilityClassifier.Classify(request.ObserverType, definition) == PartitionReplayability.Closed
            ? options!.Jobs.ProjectionRebuildBuckets
            : 0;
    }

    static bool Enabled(ChronicleOptions? options) => options?.Jobs.ProjectionRebuildBuckets > 0 && options.ReadModels.GuardSinkWritesOnWatermark;

    internal static IImmutableList<JobStepDetails> Prepare(
        ReplayObserverRequest request,
        int count,
        IList<ReplayBucketStep> progress,
        Func<HandleEventsForObserverArguments, JobStepDetails> createStep)
    {
        var steps = ImmutableList.CreateBuilder<JobStepDetails>();
        for (var index = 0; index < count; index++)
        {
            var bucket = new ReplayBucket(index, count, 1);
            var arguments = new HandleEventsForObserverArguments(request.ObserverKey, request.ObserverType, EventSequenceNumber.First, EventSequenceNumber.Max, EventObservationState.Replay, request.EventTypes)
            {
                Bucket = bucket,
                ReaderScope = request.ReaderScope,
                Revision = request.Revision
            };
            var step = createStep(arguments);
            steps.Add(step);
            progress.Add(new(step.Id, bucket, EventSequenceNumber.Unavailable));
        }

        return steps.ToImmutable();
    }
}
