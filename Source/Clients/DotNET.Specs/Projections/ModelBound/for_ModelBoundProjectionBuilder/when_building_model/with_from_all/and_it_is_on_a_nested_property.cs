// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_from_all;

public class and_it_is_on_a_nested_property : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([typeof(DetailsChanged)]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(Parent));

    [Fact] void should_preserve_the_root_mapping() => _result.All.Properties[nameof(Parent.LastValue)].ShouldEqual("$eventContext(EventSourceId)");
    [Fact] void should_keep_only_root_members_at_the_root() => _result.All.Properties.Count.ShouldEqual(1);
    [Fact] void should_put_from_all_on_the_nested_object() => _result.Nested[nameof(Parent.Details)].All.Properties[nameof(Detail.LastValue)].ShouldEqual(nameof(DetailsChanged.Value));
    [Fact] void should_put_parameter_from_every_on_the_nested_object() => _result.Nested[nameof(Parent.Details)].All.Properties[nameof(Detail.LastUpdated)].ShouldEqual("$eventContext(Occurred)");
    [Fact] void should_include_descendant_events_for_the_nested_object() => _result.Nested[nameof(Parent.Details)].All.IncludeChildren.ShouldBeTrue();
    [Fact] void should_not_subscribe_the_root_to_all_events() => _result.SubscribesToAllEvents.ShouldBeFalse();

    [EventType]
    public record DetailsChanged(string Value);

    public record Parent(
        [FromEvery(contextProperty: nameof(EventContext.EventSourceId))] string LastValue,
        [Nested] Detail? Details);

    [FromEvent<DetailsChanged>]
    public record Detail(
        [property: FromAll(property: nameof(DetailsChanged.Value))] string LastValue,
        [FromEvery(contextProperty: nameof(EventContext.Occurred))] DateTimeOffset LastUpdated);
}
