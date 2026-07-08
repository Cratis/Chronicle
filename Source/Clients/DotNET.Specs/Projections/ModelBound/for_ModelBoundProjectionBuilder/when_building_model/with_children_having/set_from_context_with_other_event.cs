// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_children_having;

public class set_from_context_with_other_event : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([typeof(WidgetInstalled), typeof(WidgetServiced)]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(WidgetsForMachine));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();
    [Fact] void should_have_children_definition() => _result.Children.Count.ShouldEqual(1);

    [Fact]
    void should_have_from_definition_for_the_creating_event()
    {
        var eventType = event_types.GetEventTypeFor(typeof(WidgetInstalled)).ToContract();
        var childrenDef = _result.Children[nameof(WidgetsForMachine.Widgets)];
        childrenDef.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_have_from_definition_for_the_other_event()
    {
        var eventType = event_types.GetEventTypeFor(typeof(WidgetServiced)).ToContract();
        var childrenDef = _result.Children[nameof(WidgetsForMachine.Widgets)];
        childrenDef.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_map_installed_at_from_the_creating_event_context()
    {
        var eventType = event_types.GetEventTypeFor(typeof(WidgetInstalled)).ToContract();
        var childrenDef = _result.Children[nameof(WidgetsForMachine.Widgets)];
        var fromDef = childrenDef.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDef.Properties.Keys.ShouldContain(nameof(InstalledWidget.InstalledAt));
        fromDef.Properties[nameof(InstalledWidget.InstalledAt)].ShouldEqual("$eventContext(Occurred)");
    }

    [Fact]
    void should_map_last_serviced_at_from_the_other_event_context()
    {
        var eventType = event_types.GetEventTypeFor(typeof(WidgetServiced)).ToContract();
        var childrenDef = _result.Children[nameof(WidgetsForMachine.Widgets)];
        var fromDef = childrenDef.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDef.Properties.Keys.ShouldContain(nameof(InstalledWidget.LastServicedAt));
        fromDef.Properties[nameof(InstalledWidget.LastServicedAt)].ShouldEqual("$eventContext(Occurred)");
    }

    [Fact]
    void should_not_bind_the_other_event_mapping_to_the_creating_event()
    {
        var eventType = event_types.GetEventTypeFor(typeof(WidgetInstalled)).ToContract();
        var childrenDef = _result.Children[nameof(WidgetsForMachine.Widgets)];
        var fromDef = childrenDef.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDef.Properties.Keys.ShouldNotContain(nameof(InstalledWidget.LastServicedAt));
    }

    [Fact]
    void should_not_bind_the_creating_event_mapping_to_the_other_event()
    {
        var eventType = event_types.GetEventTypeFor(typeof(WidgetServiced)).ToContract();
        var childrenDef = _result.Children[nameof(WidgetsForMachine.Widgets)];
        var fromDef = childrenDef.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDef.Properties.Keys.ShouldNotContain(nameof(InstalledWidget.InstalledAt));
    }
}

[EventType]
public record WidgetInstalled(WidgetName Name);

[EventType]
public record WidgetServiced;

public record MachineId(Guid Value);
public record WidgetId(Guid Value);
public record WidgetName(string Value);

public record InstalledWidget(
    WidgetId Id,
    [SetFromContext<WidgetInstalled>(nameof(EventContext.Occurred))]
    DateTimeOffset InstalledAt,
    [SetFromContext<WidgetServiced>(nameof(EventContext.Occurred))]
    DateTimeOffset LastServicedAt);

[Passive]
[FromEvent<WidgetInstalled>]
public record WidgetsForMachine(
    MachineId Id,

    [ChildrenFrom<WidgetInstalled>(identifiedBy: nameof(InstalledWidget.Id))]
    IEnumerable<InstalledWidget> Widgets);
