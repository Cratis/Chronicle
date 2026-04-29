// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using System.Text.Json;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ProjectionBuilderFor.when_building;

public class with_nested_object : Specification
{
    ProjectionBuilderFor<NestedParentReadModel> _builder;
    IEventTypes _eventTypes;
    ProjectionDefinition _result;

    void Establish()
    {
        _eventTypes = new EventTypesForSpecifications([typeof(NestedItemSet), typeof(NestedItemCleared)]);
        _builder = new ProjectionBuilderFor<NestedParentReadModel>(
            new ProjectionId(typeof(NestedParentReadModel).FullName!),
            typeof(NestedParentReadModel),
            new DefaultNamingPolicy(),
            _eventTypes,
            new JsonSerializerOptions());
    }

    void Because()
    {
        _builder.Nested(_ => _.Item, nested => nested
            .From<NestedItemSet>()
            .ClearWith<NestedItemCleared>());
        _result = _builder.Build();
    }

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();
    [Fact] void should_have_one_nested_entry() => _result.Nested.Count.ShouldEqual(1);
    [Fact] void should_have_nested_entry_for_item() => _result.Nested.Keys.ShouldContain(nameof(NestedParentReadModel.Item));

    [Fact]
    void should_have_from_definition_for_nested_item_set()
    {
        var eventType = _eventTypes.GetEventTypeFor(typeof(NestedItemSet)).ToContract();
        var nestedDef = _result.Nested[nameof(NestedParentReadModel.Item)];
        nestedDef.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_have_removed_with_for_nested_item_cleared()
    {
        var eventType = _eventTypes.GetEventTypeFor(typeof(NestedItemCleared)).ToContract();
        var nestedDef = _result.Nested[nameof(NestedParentReadModel.Item)];
        nestedDef.RemovedWith.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_not_have_children_entry_for_item() => _result.Children.Keys.ShouldNotContain(nameof(NestedParentReadModel.Item));
}

[EventType]
public record NestedItemSet(string Name, string Value);

[EventType]
public record NestedItemCleared;

public record NestedItemReadModel(string Name, string Value);

public record NestedParentReadModel(NestedItemReadModel? Item);
