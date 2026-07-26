// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_SaveChanges;

public class when_the_event_creates_the_read_model_instance : given.a_save_changes_step
{
    void Establish() => _context.IsNewInstance = true;

    async Task Because() => await CreateStep(guardWritesOnWatermark: true).Perform(_projection, _context);

    [Fact] void should_write_unconditionally() => WriteMode.ShouldEqual(SinkWriteMode.Always);
}
