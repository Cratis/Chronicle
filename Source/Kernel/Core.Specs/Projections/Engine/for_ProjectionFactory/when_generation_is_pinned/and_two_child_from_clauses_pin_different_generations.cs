// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_generation_is_pinned;

public class and_two_child_from_clauses_pin_different_generations : given.a_pinned_pipeline
{
    ProjectionEventContext[] _contexts;

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
        var pipeline = await CreatePipeline();
        _contexts = (await pipeline.HandleGenerations([Event(_second, "migrated"), Event(_first, "original")])).ToArray();
    }

    [Fact] void should_add_both_children() => ((IEnumerable<object>)_saved["parent"]["items"]!).Count().ShouldEqual(2);
    [Fact] void should_save_each_child_separately() => _writes.ShouldEqual(2);
    [Fact] void should_not_emit_conflicting_child_additions() => _contexts.All(context => context.Changeset.Changes.OfType<ChildAdded>().Count() == 1).ShouldBeTrue();
    [Fact] void should_apply_the_generations_in_ascending_order() => _contexts.Select(context => context.Event.Context.EventType).ShouldContainOnly(_first, _second);
}
