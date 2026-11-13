// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_SaveChanges;

public class when_the_projection_is_marked_for_applied_event_markers : given.a_save_changes_step
{
    void Establish()
    {
        _projection.MarkedJoins.Returns([new AppliedEventMarkerJoin("def", "type", "customerId")]);
        _projection.EventSequenceId.Returns(new EventSequenceId("the-sequence"));
    }

    async Task Because() => await CreateStep(guardWritesOnWatermark: true).Perform(_projection, _context);

    [Fact] void should_write_once_per_source_event() => WriteMode.ShouldEqual(SinkWriteMode.OncePerSourceEvent);
    [Fact] void should_record_the_markers_under_the_event_sequence_of_the_projection() => _sink.MarkedEventSequences.Single().ShouldEqual(new EventSequenceId("the-sequence"));
}
