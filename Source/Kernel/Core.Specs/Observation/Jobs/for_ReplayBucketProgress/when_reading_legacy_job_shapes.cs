// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayBucketProgress;

public class when_reading_legacy_job_shapes : Specification
{
    HandleEventsForObserverArguments _arguments;
    HandleEventsForObserverState _step;
    JobStateWithLastHandledEvent _job;

    void Because()
    {
        var arguments = new HandleEventsForObserverArguments(ObserverKey.NotSet, ObserverType.Projection, EventSequenceNumber.First, EventSequenceNumber.Max, EventObservationState.Replay, []);
        var json = JsonSerializer.SerializeToNode(arguments, ReplaySerialization.Options())!.AsObject();
        json.Remove(nameof(HandleEventsForObserverArguments.Bucket));
        _arguments = json.Deserialize<HandleEventsForObserverArguments>(ReplaySerialization.Options())!;
        _step = JsonSerializer.Deserialize<HandleEventsForObserverState>("{}", ReplaySerialization.Options())!;
        _job = JsonSerializer.Deserialize<JobStateWithLastHandledEvent>("{}", ReplaySerialization.Options())!;
    }

    [Fact] void should_read_old_arguments_as_ordered() => _arguments.Bucket.ShouldBeNull();
    [Fact] void should_read_old_step_state_as_ordered() => _step.Bucket.ShouldBeNull();
    [Fact] void should_read_old_job_state_without_bucket_progress() => _job.ReplayBucketSteps.ShouldBeEmpty();
}
