// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Contracts.EventSequences;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Identities;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class and_causation_manager_returns_null : given.an_event_sequence
{
    List<EventForEventSourceId> _events;
    EventType _eventType;
    JsonObject _eventContext;
    AppendManyRequest _command;
    AppendManyResult _result;

    void Establish()
    {
        _eventType = new(Guid.NewGuid().ToString(), EventTypeGeneration.First);
        _eventContext = [];
        _eventSerializer.Serialize(Arg.Any<string>()).Returns(_eventContext);

        _events =
        [
            new EventForEventSourceId(Guid.NewGuid(), "Event1"),
            new EventForEventSourceId(Guid.NewGuid(), "Event2")
        ];

        _eventTypes.HasFor(typeof(string)).Returns(true);
        _eventTypes.GetEventTypeFor(typeof(string)).Returns(_eventType);
        _eventSequences
            .When(_ => _.AppendMany(Arg.Any<AppendManyRequest>(), CallContext.Default))
            .Do(callInfo => _command = callInfo.Arg<AppendManyRequest>());

        _causationManager.GetCurrentChain().Returns((System.Collections.Immutable.IImmutableList<Causation>)null!);
        _identityProvider.GetCurrent().Returns(new Identity("Subject", "Name", "UserName"));

        _serviceAccessor.Services.EventSequences.AppendMany(Arg.Any<AppendManyRequest>(), CallContext.Default).Returns(new AppendManyResponse
        {
            CorrelationId = Guid.NewGuid(),
            SequenceNumbers = [42, 43],
            ConstraintViolations = [],
            Errors = []
        });
    }

    async Task Because() => _result = await _eventSequence.AppendMany(_events);

    [Fact] void should_append_events() => _command.ShouldNotBeNull();
    [Fact] void should_send_empty_causation_chain() => _command.Causation.ShouldBeEmpty();
    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
}
