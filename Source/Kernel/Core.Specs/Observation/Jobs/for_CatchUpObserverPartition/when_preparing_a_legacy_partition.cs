// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_CatchUpObserverPartition;

public class when_preparing_a_legacy_partition : given.a_partition_catch_up_job
{
    HandleEventsForPartitionArguments _arguments;

    async Task Because()
    {
        _arguments = (HandleEventsForPartitionArguments)(await _partitionJob.PrepareForTesting(new(_request.ObserverKey, _request.ObserverType, "B", 10UL, []))).Single().Request;
    }

    [Fact] void should_keep_the_legacy_unbounded_reader() => _arguments.EndEventSequenceNumber.ShouldEqual(EventSequenceNumber.Max);
}
