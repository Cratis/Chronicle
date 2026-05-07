// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Contracts.EventSequences;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Identities;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class known_event_and_causation_manager_returns_null : given.an_event_sequence
{
    EventSourceId _eventSourceId;
    string _event;
    EventType _eventType;
    AppendRequest _request;
    JsonObject _eventContext;

    void Establish()
    {
        _eventSourceId = Guid.NewGuid();
        _event = "Actual event";
        _eventType = new(Guid.NewGuid().ToString(), EventTypeGeneration.First);

        _eventContext = [];
        _eventSerializer.Serialize(_event).Returns(_eventContext);

        _eventTypes.HasFor(typeof(string)).Returns(true);
        _eventTypes.GetEventTypeFor(typeof(string)).Returns(_eventType);
        _eventSequences
            .When(_ => _.Append(Arg.Any<AppendRequest>(), CallContext.Default))
            .Do(callInfo => _request = callInfo.Arg<AppendRequest>());

        _causationManager.GetCurrentChain().Returns((System.Collections.Immutable.IImmutableList<Causation>)null!);
        _identityProvider.GetCurrent().Returns(new Identity("Subject", "Name", "UserName"));

        _serviceAccessor.Services.EventSequences.Append(Arg.Any<AppendRequest>(), CallContext.Default).Returns(new AppendResponse
        {
            CorrelationId = Guid.NewGuid(),
            SequenceNumber = 42,
            ConstraintViolations = [],
            Errors = []
        });
    }

    async Task Because() => await _eventSequence.Append(_eventSourceId, _event, concurrencyScope: ConcurrencyScope.None);

    [Fact] void should_append_event() => _request.ShouldNotBeNull();
    [Fact] void should_send_empty_causation_chain() => _request.Causation.ShouldBeEmpty();
    [Fact] void should_send_explicit_concurrency_scope() => _request.ConcurrencyScope.SequenceNumber.ShouldEqual((ulong)ConcurrencyScope.None.SequenceNumber);
}
