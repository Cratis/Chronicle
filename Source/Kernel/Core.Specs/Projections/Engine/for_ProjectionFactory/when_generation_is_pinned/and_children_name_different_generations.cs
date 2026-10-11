// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_generation_is_pinned;

public class and_children_name_different_generations : given.a_projection_with_generations
{
    ResolvedKey _key;

    void Establish()
    {
        var from = _definition.From.ToDictionary(kvp => kvp.Key, kvp => kvp.Value with { ParentKey = "$value(parent)" });
        _definition = _definition with
        {
            From = new Dictionary<Concepts.Events.EventType, FromDefinition>(),
            Children = new Dictionary<PropertyPath, ChildrenDefinition>
            {
                [(PropertyPath)"items"] = new("id", from, new Dictionary<Concepts.Events.EventType, JoinDefinition>(), new Dictionary<PropertyPath, ChildrenDefinition>(), _definition.FromEvery, _definition.RemovedWith, _definition.RemovedWithJoin, AutoMap: AutoMap.Disabled)
            }
        };
    }

    async Task Because()
    {
        _projection = await _factory.Create("store", "namespace", _definition, _readModel, _schemas);
        var child = _projection.ChildProjections.Single();
        _key = (ResolvedKey)await child.GetKeyResolverFor(_second)(_sequence, NullSink.Instance, Event(_second, "migrated"));
        Project(child, Event(_first, "original"), new ArrayIndexers([new("[items]", "id", "child")]));
        Project(child, Event(_second, "migrated"), new ArrayIndexers([new("[items]", "id", "child")]));
    }

    [Fact] void should_use_the_declared_parent_key() => _key.Key.Value.ShouldEqual("parent");
    [Fact] void should_use_the_second_generation_for_the_child_key() => _key.Key.ArrayIndexers.All.Single().Identifier.ShouldEqual("key-2");
    [Fact] void should_keep_the_first_generation_child_mapping() => Item()["oldName"].ShouldEqual("original");
    [Fact] void should_keep_the_second_generation_child_mapping() => Item()["newName"].ShouldEqual("migrated");

    IDictionary<string, object?> Item() => (IDictionary<string, object?>)((IEnumerable<object>)((IDictionary<string, object?>)_state)["items"]!).Single();
}
