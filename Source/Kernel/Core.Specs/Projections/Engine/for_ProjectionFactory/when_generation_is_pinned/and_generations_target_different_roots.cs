// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_generation_is_pinned;

public class and_generations_target_different_roots : given.a_pinned_pipeline
{
    async Task Because()
    {
        var pipeline = await CreatePipeline();
        await pipeline.Handle(Event(_first, "original"));
        await pipeline.Handle(Event(_second, "migrated"));
    }

    [Fact] void should_save_the_first_clause_under_its_own_key() => _saved["key-1"]["oldName"].ShouldEqual("original");
    [Fact] void should_save_the_second_clause_under_its_own_key() => _saved["key-2"]["newName"].ShouldEqual("migrated");
    [Fact] void should_save_both_roots() => _saved.Count.ShouldEqual(2);
}
