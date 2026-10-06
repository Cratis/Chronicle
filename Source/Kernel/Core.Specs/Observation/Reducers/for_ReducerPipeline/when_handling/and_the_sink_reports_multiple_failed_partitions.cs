// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerPipeline.when_handling;

public class and_the_sink_reports_multiple_failed_partitions : given.all_dependencies
{
    readonly FailedPartition[] _failures = [new("first", 1UL), new("second", 2UL)];
    Exception _error;

    void Establish()
    {
        _objectComparer.Compare(Arg.Any<ExpandoObject>(), Arg.Any<ExpandoObject>(), out Arg.Any<IEnumerable<PropertyDifference>>())
            .Returns(info =>
            {
                info[2] = new[] { new PropertyDifference("value", null, 1) };
                return false;
            });
        _sink.ApplyChanges(Arg.Any<Key>(), Arg.Any<IChangeset<AppendedEvent, ExpandoObject>>(), Arg.Any<EventSequenceNumber>(), Arg.Any<SinkWriteMode>()).Returns(_failures);
    }

    async Task Because() => _error = await Catch.Exception(() => _pipeline.Reduce(CreateContext(EventSourceIdValue), CreateReducer(new ExpandoObject())));

    [Fact] void should_surface_every_partition() => ((BulkWriteFailed)_error).FailedPartitions.ShouldContainOnly(_failures);
    [Fact] void should_fail_the_reduction() => _error.ShouldBeOfExactType<BulkWriteFailed>();
}
