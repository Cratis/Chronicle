// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_EventValueProviders;

public class when_getting_a_missing_on_behalf_of_identity : Expressions.given.an_appended_event
{
    object? _value;

    void Because() => _value = EventValueProviders.EventContext("CausedBy.OnBehalfOf.UserName")(@event);

    [Fact] void should_return_no_value() => _value.ShouldBeNull();
    [Fact] void should_leave_the_event_context_unchanged() => @event.Context.CausedBy.OnBehalfOf.ShouldBeNull();
}
