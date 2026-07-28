// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipeline;

public class when_many_partitions_are_handled_concurrently : given.a_striped_pipeline_over_a_shared_read_model
{
    const int Partitions = 50;
    const int EventsPerPartition = 20;

    string[] _eventSourceIds;

    void Establish() => _eventSourceIds = Enumerable.Range(0, Partitions).Select(index => $"partition-{index}").ToArray();

    async Task Because()
    {
        var handled = _eventSourceIds
            .SelectMany(eventSourceId => Enumerable.Range(0, EventsPerPartition).Select(_ => _pipeline.Handle(EventFor(eventSourceId))))
            .ToArray();

        await Task.WhenAll(handled);
    }

    [Fact]
    void should_handle_events_for_every_partition() => _counters.Count.ShouldEqual(Partitions);

    [Fact]
    void should_apply_every_event_to_its_partition_without_losing_a_write() =>
        _eventSourceIds.All(eventSourceId => _counters[eventSourceId].ReadModelValue == EventsPerPartition).ShouldBeTrue();

    [Fact]
    void should_never_handle_two_events_for_the_same_partition_concurrently() =>
        _counters.Values.All(partition => partition.Max == 1).ShouldBeTrue();
}
