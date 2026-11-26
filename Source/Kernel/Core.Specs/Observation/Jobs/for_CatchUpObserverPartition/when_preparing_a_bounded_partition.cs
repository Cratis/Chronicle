// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_CatchUpObserverPartition;

public class when_preparing_a_bounded_partition : given.a_partition_catch_up_job
{
    HandleEventsForPartitionArguments _arguments;

    async Task Because()
    {
        var request = new CatchUpObserverPartitionRequest(_request.ObserverKey, _request.ObserverType, "B", 10UL, []) { ToSequenceNumber = 12UL };
        _arguments = (HandleEventsForPartitionArguments)(await _partitionJob.PrepareForTesting(request)).Single().Request;
    }

    [Fact] void should_preserve_the_start() => _arguments.StartEventSequenceNumber.ShouldEqual((EventSequenceNumber)10UL);
    [Fact] void should_limit_the_reader_to_the_requested_inclusive_end() => _arguments.EndEventSequenceNumber.ShouldEqual((EventSequenceNumber)12UL);
}
