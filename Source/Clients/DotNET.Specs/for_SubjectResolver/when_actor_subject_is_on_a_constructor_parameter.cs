// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.for_SubjectResolver;

public class when_actor_subject_is_on_a_constructor_parameter : Specification
{
    record OrderId(string TypedValue) : EventSourceId<string>(TypedValue);
    record ActorId(string TypedValue) : EventSourceId<string>(TypedValue);
    record OrderPlaced(OrderId Id, [Subject] ActorId Actor);

    OrderPlaced _event;
    Subject? _result;

    void Establish() => _event = new(new OrderId("order-42"), new ActorId("actor-77"));

    void Because() => _result = SubjectResolver.ResolveFrom(_event);

    [Fact] void should_resolve_the_actor_instead_of_the_event_source() => _result.ShouldEqual(new Subject("actor-77"));
}
