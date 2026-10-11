// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_pinned_generations;

public class and_two_child_from_clauses_target_the_same_array : given.a_pinned_pipeline
{
    void Establish() => _definition = _definition with
    {
        From = new Dictionary<EventType, FromDefinition>(),
        Children = new Dictionary<PropertyPath, ChildrenDefinition>
        {
            [(PropertyPath)"items"] = new("id", _definition.From.ToDictionary(kvp => kvp.Key, kvp => kvp.Value with { ParentKey = "$value(parent)" }), new Dictionary<EventType, JoinDefinition>(), new Dictionary<PropertyPath, ChildrenDefinition>(), _definition.FromEvery, _definition.RemovedWith, _definition.RemovedWithJoin, AutoMap: AutoMap.Disabled)
        }
    };

    async Task Because()
    {
        await CreatePipeline();
        await _pipeline.HandleGenerations([Event(_second, "migrated"), Event(_first, "original")]);
    }

    [Fact] void should_write_two_non_conflicting_pushes() => _rootUpdates.Count.ShouldEqual(2);
    [Fact] void should_add_the_first_generation_child() => _rootUpdates[0]["$push"]["items"]["_id"].AsString.ShouldEqual("key-1");
    [Fact] void should_add_the_second_generation_child() => _rootUpdates[^1]["$push"]["items"]["_id"].AsString.ShouldEqual("key-2");
}
