// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using System.Text.Json;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ProjectionBuilderFor.when_building;

public class with_nested_object_in_nested_object : Specification
{
    ProjectionBuilderFor<RecursiveNestedParentReadModel> _builder;
    IEventTypes _eventTypes;
    ProjectionDefinition _result;

    void Establish()
    {
        _eventTypes = new EventTypesForSpecifications([typeof(RecursiveNestedItemSet), typeof(RecursiveNestedItemCleared), typeof(RecursiveNestedValidationConfigured), typeof(RecursiveNestedValidationCleared)]);
        _builder = new ProjectionBuilderFor<RecursiveNestedParentReadModel>(
            new ProjectionId(typeof(RecursiveNestedParentReadModel).FullName),
            typeof(RecursiveNestedParentReadModel),
            new DefaultNamingPolicy(),
            _eventTypes,
            new JsonSerializerOptions());
    }

    void Because()
    {
        _builder.Nested(_ => _.Item, item => item
            .From<RecursiveNestedItemSet>()
            .Nested(_ => _.Validation, validation => validation
                .From<RecursiveNestedValidationConfigured>()
                .ClearWith<RecursiveNestedValidationCleared>())
            .ClearWith<RecursiveNestedItemCleared>());

        _result = _builder.Build();
    }

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();
    [Fact] void should_have_nested_entry_for_item() => _result.Nested.Keys.ShouldContain(nameof(RecursiveNestedParentReadModel.Item));
    [Fact] void should_have_nested_entry_for_validation_on_item() => _result.Nested[nameof(RecursiveNestedParentReadModel.Item)].Nested.Keys.ShouldContain(nameof(RecursiveNestedItemReadModel.Validation));

    [Fact]
    void should_have_from_definition_for_nested_item_set()
    {
        var eventType = _eventTypes.GetEventTypeFor(typeof(RecursiveNestedItemSet)).ToContract();
        var itemDefinition = _result.Nested[nameof(RecursiveNestedParentReadModel.Item)];
        itemDefinition.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_have_removed_with_definition_for_nested_item_clear()
    {
        var eventType = _eventTypes.GetEventTypeFor(typeof(RecursiveNestedItemCleared)).ToContract();
        var itemDefinition = _result.Nested[nameof(RecursiveNestedParentReadModel.Item)];
        itemDefinition.RemovedWith.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_have_from_definition_for_nested_validation_configured()
    {
        var eventType = _eventTypes.GetEventTypeFor(typeof(RecursiveNestedValidationConfigured)).ToContract();
        var validationDefinition = _result.Nested[nameof(RecursiveNestedParentReadModel.Item)].Nested[nameof(RecursiveNestedItemReadModel.Validation)];
        validationDefinition.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_have_removed_with_definition_for_nested_validation_clear()
    {
        var eventType = _eventTypes.GetEventTypeFor(typeof(RecursiveNestedValidationCleared)).ToContract();
        var validationDefinition = _result.Nested[nameof(RecursiveNestedParentReadModel.Item)].Nested[nameof(RecursiveNestedItemReadModel.Validation)];
        validationDefinition.RemovedWith.Keys.ShouldContain(et => et.IsEqual(eventType));
    }
}

[EventType]
public record RecursiveNestedItemSet(string Name, string Value);

[EventType]
public record RecursiveNestedItemCleared;

[EventType]
public record RecursiveNestedValidationConfigured(string Rules);

[EventType]
public record RecursiveNestedValidationCleared;

public record RecursiveNestedValidationReadModel(string Rules);

public record RecursiveNestedItemReadModel(string Name, string Value, RecursiveNestedValidationReadModel? Validation);

public record RecursiveNestedParentReadModel(RecursiveNestedItemReadModel? Item);

#pragma warning restore SA1402 // File may only contain a single type
