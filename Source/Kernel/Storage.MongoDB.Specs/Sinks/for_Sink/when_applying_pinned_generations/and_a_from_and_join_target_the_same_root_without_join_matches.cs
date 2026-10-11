// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_pinned_generations;

public class and_a_from_and_join_target_the_same_root_without_join_matches : given.a_pinned_pipeline
{
    void Establish() => _definition = _definition with
    {
        From = new Dictionary<EventType, FromDefinition> { [_second] = _definition.From[_second] with { Key = "$value(source)" } },
        Join = new Dictionary<EventType, JoinDefinition> { [_first] = new("joinId", new Dictionary<PropertyPath, string> { [(PropertyPath)"oldName"] = "name" }, PropertyExpression.NotSet) }
    };

    async Task Because()
    {
        await CreatePipeline();
        await _pipeline.HandleGenerations([Event(_second, "migrated"), Event(_first, "original")]);
    }

    [Fact] void should_apply_the_join_mapping() => _joinUpdates.Single()["$set"]["oldName"].AsString.ShouldEqual("original");
    [Fact] void should_apply_the_keyed_from_mapping_even_without_a_join_target() => _rootUpdates.Single()["$set"]["newName"].AsString.ShouldEqual("migrated");
    [Fact] void should_allow_the_from_to_create_the_root() => _upserts.Single().ShouldBeTrue();
}
