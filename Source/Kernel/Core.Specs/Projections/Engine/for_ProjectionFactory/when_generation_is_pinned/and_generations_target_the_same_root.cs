// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_generation_is_pinned;

public class and_generations_target_the_same_root : given.a_pinned_pipeline
{
    void Establish() => _definition = _definition with { From = _definition.From.ToDictionary(kvp => kvp.Key, kvp => kvp.Value with { Key = "$value(root)" }) };

    async Task Because()
    {
        var pipeline = await CreatePipeline();
        await pipeline.Handle(Event(_first, "original"));
        await pipeline.Handle(Event(_second, "migrated"));
    }

    [Fact] void should_save_the_first_clause() => _saved["root"]["oldName"].ShouldEqual("original");
    [Fact] void should_save_the_second_clause_after_the_first() => _saved["root"]["newName"].ShouldEqual("migrated");
    [Fact] void should_save_each_event_separately() => _writes.ShouldEqual(2);
}
