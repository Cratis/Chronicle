// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerPipeline.when_handling;

/// <summary>
/// A reducer folds against the state it just read, so a redelivered batch would fold it a second time. The write
/// is therefore guarded on the read model's watermark whenever the instance already exists.
/// </summary>
public class and_the_read_model_already_exists : given.a_pipeline_with_a_recording_sink
{
    void Establish() => _sink.Existing = new ExpandoObject();

    async Task Because() => await _pipeline.Reduce(CreateContext(), CreateReducer(NewState()));

    [Fact] void should_guard_the_write_on_the_watermark() => _sink.WriteModes.Single().ShouldEqual(SinkWriteMode.OnlyWhenAdvancingWatermark);
}
