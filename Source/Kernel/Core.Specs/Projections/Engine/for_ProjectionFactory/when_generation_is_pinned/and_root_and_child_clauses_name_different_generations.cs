// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_generation_is_pinned;

public class and_root_and_child_clauses_name_different_generations : given.a_pinned_pipeline
{
    void Establish()
    {
        _definition = _definition with
        {
            From = new Dictionary<EventType, FromDefinition> { [_first] = _definition.From[_first] with { Key = "$value(root)" } },
            Children = new Dictionary<PropertyPath, ChildrenDefinition>
            {
                [(PropertyPath)"items"] = new("id", new Dictionary<EventType, FromDefinition> { [_second] = _definition.From[_second] with { ParentKey = "$value(root)" } }, new Dictionary<EventType, JoinDefinition>(), new Dictionary<PropertyPath, ChildrenDefinition>(), _definition.FromEvery, _definition.RemovedWith, _definition.RemovedWithJoin, AutoMap: AutoMap.Disabled)
            }
        };
    }

    async Task Because()
    {
        var pipeline = await CreatePipeline();
        await pipeline.Handle(Event(_first, "original"));
        await pipeline.Handle(Event(_second, "migrated"));
    }

    [Fact] void should_project_the_root_from_its_generation() => _saved["root"]["oldName"].ShouldEqual("original");
    [Fact] void should_project_the_child_from_its_generation() => ((IDictionary<string, object?>)((IEnumerable<object>)_saved["root"]["items"]!).Single())["newName"].ShouldEqual("migrated");
    [Fact] void should_save_the_root_and_child_separately() => _writes.ShouldEqual(2);
}
