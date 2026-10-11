// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_generation_is_pinned;

public class and_the_root_and_parent_key_have_different_representations : given.a_pinned_pipeline
{
    const string Root = "8B73A818-89C9-4D79-A3BA-B77201598269";

    void Establish()
    {
        _readModel.GetSchemaForLatestGeneration().Properties["id"].Format = "guid";
        _definition = _definition with
        {
            From = new Dictionary<EventType, FromDefinition> { [_first] = _definition.From[_first] with { Key = PropertyExpression.NotSet } },
            Children = new Dictionary<PropertyPath, ChildrenDefinition>
            {
                [(PropertyPath)"items"] = new("id", new Dictionary<EventType, FromDefinition> { [_second] = _definition.From[_second] with { ParentKey = $"$value({Root})" } }, new Dictionary<EventType, JoinDefinition>(), new Dictionary<PropertyPath, ChildrenDefinition>(), _definition.FromEvery, _definition.RemovedWith, _definition.RemovedWithJoin, AutoMap: AutoMap.Disabled)
            }
        };
    }

    async Task Because()
    {
        var pipeline = await CreatePipeline();
        var first = Event(_first, "original");
        first = first with { Context = first.Context with { EventSourceId = Root } };
        var second = Event(_second, "migrated");
        second = second with { Context = second.Context with { EventSourceId = Root } };
        await pipeline.Handle(first);
        await pipeline.Handle(second);
    }

    [Fact] void should_save_each_event_separately() => _writes.ShouldEqual(2);
    [Fact] void should_project_the_root() => _saved.Values.Single()["oldName"].ShouldEqual("original");
    [Fact] void should_project_the_child() => ((IDictionary<string, object?>)((IEnumerable<object>)_saved.Values.Single()["items"]!).Single())["newName"].ShouldEqual("migrated");
}
