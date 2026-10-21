// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_generation_is_pinned;

public class and_generations_target_their_event_source : given.a_pinned_pipeline
{
    void Establish() => _definition = _definition with { From = _definition.From.ToDictionary(kvp => kvp.Key, kvp => kvp.Value with { Key = PropertyExpression.NotSet }) };

    async Task Because()
    {
        var pipeline = await CreatePipeline();
        await pipeline.Handle(Event(_first, "original"));
        var second = Event(_second, "migrated");
        await pipeline.Handle(second with { Context = second.Context with { SequenceNumber = 1 } });
    }

    [Fact] void should_keep_event_source_keyed_handling() => _projection.IsEventSourceKeyed.ShouldBeTrue();
    [Fact] void should_save_the_first_mapping() => _saved["source"]["oldName"].ShouldEqual("original");
    [Fact] void should_save_the_second_mapping() => _saved["source"]["newName"].ShouldEqual("migrated");
    [Fact] void should_guard_the_existing_instances_watermark() => _writeModes.ShouldContainOnly(SinkWriteMode.Always, SinkWriteMode.OnlyWhenAdvancingWatermark);
}
