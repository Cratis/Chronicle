// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_set_once;

public class on_a_child : given.a_model_bound_projection_builder
{
    FromDefinition _fromRenamed;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([typeof(LineAdded), typeof(LineRenamed)]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because()
    {
        var result = builder.Build(typeof(Order));
        var eventType = event_types.GetEventTypeFor(typeof(LineRenamed)).ToContract();
        _fromRenamed = result.Children[nameof(Order.Lines)].From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
    }

    [Fact] void should_wrap_the_event_property_in_set_once() => _fromRenamed.Properties[nameof(Line.FirstName)].ShouldEqual("$setOnce(Name)");
    [Fact] void should_wrap_the_event_context_property_in_set_once() => _fromRenamed.Properties[nameof(Line.FirstRenamed)].ShouldEqual("$setOnce($eventContext(Occurred))");
}

[EventType]
public record LineAdded(Guid LineId, string Name);

[EventType]
public record LineRenamed(Guid LineId, string Name);

public record Line(
    [Key] Guid LineId,
    [SetFrom<LineRenamed>(nameof(LineRenamed.Name), SetOnce = true)] string FirstName,
    [SetFromContext<LineRenamed>(nameof(EventContext.Occurred), SetOnce = true)] DateTimeOffset FirstRenamed);

public record Order(
    [Key] Guid Id,
    [ChildrenFrom<LineAdded>(key: nameof(LineAdded.LineId))] IEnumerable<Line> Lines);
