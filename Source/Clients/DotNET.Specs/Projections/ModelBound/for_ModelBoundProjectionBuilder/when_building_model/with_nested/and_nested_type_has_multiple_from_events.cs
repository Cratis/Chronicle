// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_nested;

public class and_nested_type_has_multiple_from_events : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([
            typeof(CommandSetForSlice),
            typeof(SliceCommandRenamed),
            typeof(SliceCommandSchemaUpdated)
        ]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(SliceWithMultiEventNestedCommand));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();
    [Fact] void should_have_one_nested_entry() => _result.Nested.Count.ShouldEqual(1);

    [Fact]
    void should_have_from_definition_for_command_set()
    {
        var eventType = event_types.GetEventTypeFor(typeof(CommandSetForSlice)).ToContract();
        var nestedDef = _result.Nested[nameof(SliceWithMultiEventNestedCommand.Command)];
        nestedDef.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_have_from_definition_for_command_renamed()
    {
        var eventType = event_types.GetEventTypeFor(typeof(SliceCommandRenamed)).ToContract();
        var nestedDef = _result.Nested[nameof(SliceWithMultiEventNestedCommand.Command)];
        nestedDef.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_have_from_definition_for_schema_updated()
    {
        var eventType = event_types.GetEventTypeFor(typeof(SliceCommandSchemaUpdated)).ToContract();
        var nestedDef = _result.Nested[nameof(SliceWithMultiEventNestedCommand.Command)];
        nestedDef.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_have_three_from_entries()
    {
        var nestedDef = _result.Nested[nameof(SliceWithMultiEventNestedCommand.Command)];
        nestedDef.From.Count.ShouldEqual(3);
    }
}

[EventType]
public record CommandSetForSlice(string Name, string Schema);

[EventType]
public record SliceCommandRenamed(string Name);

[EventType]
public record SliceCommandSchemaUpdated(string Schema);

[FromEvent<CommandSetForSlice>]
[FromEvent<SliceCommandRenamed>]
[FromEvent<SliceCommandSchemaUpdated>]
public record MultiEventNestedCommandItem(string Name, string Schema);

public record SliceWithMultiEventNestedCommand(
    string SliceName,
    [Nested] MultiEventNestedCommandItem? Command);
