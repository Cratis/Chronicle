// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_nested;

public class and_nested_type_has_clear_with : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([typeof(SliceCommandSet2), typeof(SliceCommandCleared)]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(SliceWithClearableCommand));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();
    [Fact] void should_have_one_nested_entry() => _result.Nested.Count.ShouldEqual(1);

    [Fact]
    void should_have_removed_with_entry_for_slice_command_cleared()
    {
        var eventType = event_types.GetEventTypeFor(typeof(SliceCommandCleared)).ToContract();
        var nestedDef = _result.Nested[nameof(SliceWithClearableCommand.Command)];
        nestedDef.RemovedWith.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_have_no_other_removed_with_entries()
    {
        var nestedDef = _result.Nested[nameof(SliceWithClearableCommand.Command)];
        nestedDef.RemovedWith.Count.ShouldEqual(1);
    }
}

[EventType]
public record SliceCommandSet2(string Name);

[EventType]
public record SliceCommandCleared;

[FromEvent<SliceCommandSet2>]
[ClearWith<SliceCommandCleared>]
public record ClearableNestedCommandItem(string Name);

public record SliceWithClearableCommand(
    string SliceName,
    [Nested] ClearableNestedCommandItem? Command);
