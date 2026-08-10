// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.SideEffects.for_EventResultHandler;

/// <summary>
/// A caller compiled against the previous public handler surface constructs it with an event type registry and
/// calls the event-store-less <c>CanHandle</c>. Retaining only the interface default is not enough: a concrete call
/// binds to the class member and the constructor in metadata.
/// </summary>
public class when_the_previous_constructor_and_contract_are_used : Specification
{
    IEventTypes _eventTypes;
    EventResultHandler _handler;
    bool _result;

    void Establish()
    {
        _eventTypes = Substitute.For<IEventTypes>();
        _eventTypes.HasFor(typeof(SomeEvent)).Returns(true);

        _handler = new EventResultHandler(_eventTypes);
    }

    void Because() => _result = _handler.CanHandle(
        new ReactorContext(EventContext.Empty, new object(), ReactorContextValues.Empty),
        new SomeEvent());

    [Fact] void should_use_the_registry_the_caller_supplied() => _eventTypes.Received(1).HasFor(typeof(SomeEvent));
    [Fact] void should_keep_answering_the_previous_call() => _result.ShouldBeTrue();

    record SomeEvent;
}
