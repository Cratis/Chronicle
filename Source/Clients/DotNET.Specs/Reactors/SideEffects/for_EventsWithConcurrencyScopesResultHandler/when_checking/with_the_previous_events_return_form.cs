// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactors.SideEffects.for_EventsWithConcurrencyScopesResultHandler.when_checking;

public class with_the_previous_events_return_form : Specification
{
    bool _result;

    void Because() => _result = new EventsWithConcurrencyScopesResultHandler().CanHandle(
        new(Events.EventContext.Empty, new object(), ReactorContextValues.Empty),
        Array.Empty<EventForEventSourceId>());

    [Fact] void should_leave_the_value_for_the_existing_handler() => _result.ShouldBeFalse();
}
