// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_nested;

public class and_nested_type_has_from_event : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([typeof(SliceCommandSet)]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(SliceWithNestedCommand));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();
    [Fact] void should_have_one_nested_entry() => _result.Nested.Count.ShouldEqual(1);
    [Fact] void should_have_nested_entry_for_command() => _result.Nested.Keys.ShouldContain(nameof(SliceWithNestedCommand.Command));

    [Fact]
    void should_have_from_definition_for_slice_command_set()
    {
        var eventType = event_types.GetEventTypeFor(typeof(SliceCommandSet)).ToContract();
        var nestedDef = _result.Nested[nameof(SliceWithNestedCommand.Command)];
        nestedDef.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_have_auto_map_enabled_on_nested()
    {
        var nestedDef = _result.Nested[nameof(SliceWithNestedCommand.Command)];
        nestedDef.AutoMap.ShouldEqual(Contracts.Projections.AutoMap.Enabled);
    }
}

[EventType]
public record SliceCommandSet(string Name, string Schema);

[FromEvent<SliceCommandSet>]
public record NestedCommandItem(string Name, string Schema);

public record SliceWithNestedCommand(
    string SliceName,
    [Nested] NestedCommandItem? Command);
