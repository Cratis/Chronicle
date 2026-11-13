// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_SaveChanges;

public class when_the_projection_is_marked_and_markers_are_turned_off : given.a_save_changes_step
{
    void Establish()
    {
        _projection.MarkedJoins.Returns([new AppliedEventMarkerJoin("def", "type", "customerId")]);
        _projection.IsEventSourceKeyed.Returns(false);
    }

    async Task Because() => await CreateStep(guardWritesOnWatermark: true, recordAppliedEventMarkers: false).Perform(_projection, _context);

    [Fact] void should_write_unconditionally() => WriteMode.ShouldEqual(SinkWriteMode.Always);
}
