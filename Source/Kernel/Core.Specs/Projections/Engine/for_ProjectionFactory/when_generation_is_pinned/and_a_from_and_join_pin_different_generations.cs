// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_generation_is_pinned;

public class and_a_from_and_join_pin_different_generations : given.a_pinned_pipeline
{
    ProjectionEventContext[] _contexts;

    void Establish() => _definition = _definition with
    {
        From = new Dictionary<EventType, FromDefinition> { [_second] = _definition.From[_second] with { Key = "$value(source)" } },
        Join = new Dictionary<EventType, JoinDefinition> { [_first] = new("joinId", new Dictionary<PropertyPath, string> { [(PropertyPath)"oldName"] = "name" }, PropertyExpression.NotSet) }
    };

    async Task Because()
    {
        var pipeline = await CreatePipeline();
        _contexts = (await pipeline.HandleGenerations([Event(_second, "migrated"), Event(_first, "original")])).ToArray();
    }

    [Fact] void should_apply_both_generations() => _contexts.Length.ShouldEqual(2);
    [Fact] void should_apply_the_join_generation_first() => _contexts[0].Event.Context.EventType.ShouldEqual(_first);
    [Fact] void should_keep_the_join_in_its_own_changeset() => _contexts[0].Changeset.Changes.OfType<Joined>().Count().ShouldEqual(1);
    [Fact] void should_keep_the_from_outside_the_join_changeset() => _contexts[^1].Changeset.HasJoined().ShouldBeFalse();
    [Fact] void should_save_the_from_mapping() => _saved["source"]["newName"].ShouldEqual("migrated");
}
