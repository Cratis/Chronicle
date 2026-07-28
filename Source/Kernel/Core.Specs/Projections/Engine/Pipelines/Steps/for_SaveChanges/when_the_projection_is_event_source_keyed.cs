// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_SaveChanges;

public class when_the_projection_is_event_source_keyed : given.a_save_changes_step
{
    async Task Because() => await CreateStep(guardWritesOnWatermark: true).Perform(_projection, _context);

    [Fact] void should_guard_the_write_on_the_watermark() => WriteMode.ShouldEqual(SinkWriteMode.OnlyWhenAdvancingWatermark);
}
