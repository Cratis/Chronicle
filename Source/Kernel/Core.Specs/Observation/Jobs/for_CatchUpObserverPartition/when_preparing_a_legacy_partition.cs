// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_CatchUpObserverPartition;

public class when_preparing_a_legacy_partition : for_CatchUpObserver.given.a_catch_up_observer_job
{
    HandleEventsForPartitionArguments _arguments;

    async Task Because()
    {
        var job = await _silo.CreateGrainAsync<TestableCatchUpObserverPartition>(_jobId, _jobKey);
        _arguments = (HandleEventsForPartitionArguments)(await job.PrepareForTesting(new(_request.ObserverKey, _request.ObserverType, "B", 10UL, []))).Single().Request;
    }

    [Fact] void should_keep_the_legacy_unbounded_reader() => _arguments.EndEventSequenceNumber.ShouldEqual(EventSequenceNumber.Max);
}
