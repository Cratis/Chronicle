// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Identities;

namespace Cratis.Chronicle.Reactors.for_ReactorHandler;

public class when_handling_on_next : given.a_reactor_handler
{
    EventSequenceNumber _eventSequenceNumber;
    EventContext _eventContext;
    IDictionary<string, string> _causationProperties;
    SomeEvent _eventContent;
    object _reactorInstance;

    void Establish()
    {
        _eventSequenceNumber = 42;

        _eventContent = new("Forty two");
        _reactorInstance = new object();

        _eventContext = EventContext.Empty with
        {
            EventType = new(Guid.NewGuid().ToString(), 1),
            SequenceNumber = _eventSequenceNumber,
        };

        _causationManager
            .When(_ => _.Add(ReactorHandler.CausationType, Arg.Any<IDictionary<string, string>>()))
            .Do(callInfo => _causationProperties = callInfo.Arg<IDictionary<string, string>>());
    }

    async Task Because() => await handler.OnNext(_eventContext, _eventContent, _reactorInstance);

    [Fact] void should_add_causation() => _causationManager.Received(1).Add(ReactorHandler.CausationType, Arg.Any<IDictionary<string, string>>());
    [Fact] void should_add_causation_with_observer_id() => _causationProperties[ReactorHandler.CausationReactorIdProperty].ShouldEqual(_reactorId.ToString());
    [Fact] void should_add_causation_with_event_type_id() => _causationProperties[ReactorHandler.CausationEventTypeIdProperty].ShouldEqual(_eventContext.EventType.Id.Value);
    [Fact] void should_add_causation_with_event_type_generation() => _causationProperties[ReactorHandler.CausationEventTypeGenerationProperty].ShouldEqual(_eventContext.EventType.Generation.ToString());
    [Fact] void should_add_causation_with_event_sequence_id() => _causationProperties[ReactorHandler.CausationEventSequenceIdProperty].ShouldEqual(_eventSequenceId.ToString());
    [Fact] void should_add_causation_with_event_sequence_number() => _causationProperties[ReactorHandler.CausationEventSequenceNumberProperty].ShouldEqual(_eventContext.SequenceNumber.Value.ToString());
    [Fact] void should_invoke_observer_invoker() => _reactorInvoker.Received(1).Invoke(_reactorInstance, _eventContent, _eventContext);
    [Fact] void should_set_current_identity() => _identityProvider.Received(1).SetCurrentIdentity(Arg.Is<Identity>(i => i.Subject == Identity.System.Subject && i.OnBehalfOf == _eventContext.CausedBy));
    [Fact] void should_clear_current_identity() => _identityProvider.Received(1).ClearCurrentIdentity();
}
