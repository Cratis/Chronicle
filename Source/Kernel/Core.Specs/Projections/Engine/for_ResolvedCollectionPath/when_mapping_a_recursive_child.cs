// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ResolvedCollectionPath;

public class when_mapping_a_recursive_child : given.a_recursive_collection
{
    AppendedEvent _event;
    ExpandoObject _state;
    ArrayIndexers _indexers;
    PropertyMapper<AppendedEvent, ExpandoObject> _map;
    PropertyDifference _difference;
    int _mapperCreations;

    void Establish()
    {
        _parent.ChildrenPropertyPath.Returns((PropertyPath)"[features]");
        _recursive.ChildrenPropertyPath.Returns((PropertyPath)"[features].[subFeatures]");
        _slices.ChildrenPropertyPath.Returns((PropertyPath)"[features].[slices]");
        _state = new
        {
            features = new[]
            {
                new
                {
                    id = "feature",
                    subFeatures = new[] { new { id = "sub-feature", slices = new[] { new { id = "slice", name = "before" } } } }
                }
            }
        }.AsExpandoObject();
        _event = new AppendedEvent(EventContext.EmptyWithEventSourceId("slice"), new ExpandoObject());
        _indexers = new ArrayIndexers(
            [
                new("[features]", "id", "feature"), new("[features].[subFeatures]", "id", "sub-feature"),
                new("[features].[subFeatures].[slices]", "id", "slice")
            ]);
        _map = ResolvedCollectionPath.Mapper(_slices, "[features].[slices].name", property =>
        {
            _mapperCreations++;
            return PropertyMappers.FromEventValueProvider(property, _ => "mapped");
        });
    }

    void Because()
    {
        _map(_event, _state, _indexers);
        _difference = _map(_event, _state, _indexers);
    }

    [Fact] void should_map_the_child_at_its_resolved_depth() => _difference.PropertyPath.Path.ShouldEqual("[features].[subFeatures].[slices].name");
    [Fact] void should_apply_its_property_mapper() => ((PropertyPath)"[features].[subFeatures].[slices].name").GetValue(_state, _indexers).ShouldEqual("mapped");
    [Fact] void should_cache_the_mapper_for_the_resolved_collection() => _mapperCreations.ShouldEqual(2);
}
