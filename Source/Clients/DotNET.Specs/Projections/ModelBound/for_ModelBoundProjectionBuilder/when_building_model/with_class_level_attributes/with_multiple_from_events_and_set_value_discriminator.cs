// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_class_level_attributes;

/// <summary>
/// Regression test for: "Multi-[SetValue] on a projected property only applies the first attribute".
/// A read-model that declares two class-level [FromEvent] attributes and then uses [SetValue] to assign
/// a distinct constant to one enum property depending on which event fires must produce a separate
/// FromDefinition for every event type, each with the correct $value(...) expression.
/// </summary>
public class with_multiple_from_events_and_set_value_discriminator : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    enum RoleKind
    {
        UI = 0,
        System = 1
    }

    [EventType]
    record RoleAddedAsUIEvent(Guid CollectionId, Guid RoleId, string Name);

    [EventType]
    record RoleAddedAsSystemEvent(Guid CollectionId, Guid RoleId, string Name);

    [FromEvent<RoleAddedAsUIEvent>(key: nameof(RoleAddedAsUIEvent.RoleId))]
    [FromEvent<RoleAddedAsSystemEvent>(key: nameof(RoleAddedAsSystemEvent.RoleId))]
    record MultiSetValueRole(
        [Key] Guid Id,
        string Name,
        [SetValue<RoleAddedAsUIEvent>(RoleKind.UI)]
        [SetValue<RoleAddedAsSystemEvent>(RoleKind.System)]
        RoleKind Kind);

    void Establish()
    {
        event_types = new EventTypesForSpecifications([
            typeof(RoleAddedAsUIEvent),
            typeof(RoleAddedAsSystemEvent)
        ]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(MultiSetValueRole));

    [Fact]
    void should_have_two_from_definitions() => _result.From.Count.ShouldEqual(2);

    [Fact]
    void should_set_ui_kind_when_ui_event_fires()
    {
        var eventType = event_types.GetEventTypeFor(typeof(RoleAddedAsUIEvent)).ToContract();
        var fromDefinition = _result.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDefinition.Properties[nameof(MultiSetValueRole.Kind)].ShouldEqual("$value(0)");
    }

    [Fact]
    void should_set_system_kind_when_system_event_fires()
    {
        var eventType = event_types.GetEventTypeFor(typeof(RoleAddedAsSystemEvent)).ToContract();
        var fromDefinition = _result.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDefinition.Properties[nameof(MultiSetValueRole.Kind)].ShouldEqual("$value(1)");
    }
}
