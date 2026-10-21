// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Seeding.for_EventSeeding;

public class when_seeding_a_derived_event_with_typed_routing : given.a_seeding_builder
{
    async Task Because()
    {
        _seeding.For<TestEvent>("source", "Lines", "line-1", [new DerivedEvent("value")], "Order");
        await _seeding.Register();
    }

    [Fact] void should_use_the_declared_generic_event_type_like_the_unrouted_overload() => _request.GlobalByEventSource.Single().Entries.Single().EventTypeId.ShouldEqual("test-event");

    record DerivedEvent(string Value) : TestEvent(Value);
}
