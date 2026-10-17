// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_from_all;

public class and_it_is_on_a_child_property : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([typeof(ChildAdded)]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(Parent));

    [Fact] void should_put_from_all_on_the_child() => _result.Children[nameof(Parent.Children)].All.Properties[nameof(Child.LastValue)].ShouldEqual(nameof(ChildAdded.Value));
    [Fact] void should_put_parameter_from_every_on_the_child() => _result.Children[nameof(Parent.Children)].All.Properties[nameof(Child.LastUpdated)].ShouldEqual("$eventContext(Occurred)");
    [Fact] void should_include_descendant_events_for_the_child() => _result.Children[nameof(Parent.Children)].All.IncludeChildren.ShouldBeTrue();
    [Fact] void should_not_put_child_mappings_on_the_root() => _result.All.Properties.ShouldBeEmpty();
    [Fact] void should_not_enable_root_child_inclusion() => _result.All.IncludeChildren.ShouldBeFalse();
    [Fact] void should_not_subscribe_the_root_to_all_events() => _result.SubscribesToAllEvents.ShouldBeFalse();

    [EventType]
    public record ChildAdded(Guid ChildId, string Value);

    public record Parent(
        [NotProjected] string LastValue,
        [ChildrenFrom<ChildAdded>(key: nameof(ChildAdded.ChildId))] IEnumerable<Child> Children);

    public record Child(
        [Key] Guid Id,
        [property: FromAll(property: nameof(ChildAdded.Value))] string LastValue,
        [FromEvery(contextProperty: nameof(EventContext.Occurred))] DateTimeOffset LastUpdated);
}
