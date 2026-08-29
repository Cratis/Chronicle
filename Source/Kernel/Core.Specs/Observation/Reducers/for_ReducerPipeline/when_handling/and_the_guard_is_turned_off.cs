// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerPipeline.when_handling;

public class and_the_guard_is_turned_off : given.a_pipeline_with_a_recording_sink
{
    protected override bool GuardWritesOnWatermark => false;

    void Establish() => _sink.Existing = new ExpandoObject();

    async Task Because() => await _pipeline.Reduce(CreateContext(), CreateReducer(NewState()));

    [Fact] void should_write_unconditionally() => _sink.WriteModes.Single().ShouldEqual(SinkWriteMode.Always);
}
