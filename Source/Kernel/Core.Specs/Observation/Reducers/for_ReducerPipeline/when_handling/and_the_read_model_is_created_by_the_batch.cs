// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerPipeline.when_handling;

public class and_the_read_model_is_created_by_the_batch : given.a_pipeline_with_a_recording_sink
{
    async Task Because() => await _pipeline.Reduce(CreateContext(), CreateReducer(NewState()));

    [Fact] void should_write_unconditionally() => _sink.WriteModes.Single().ShouldEqual(SinkWriteMode.Always);
}
