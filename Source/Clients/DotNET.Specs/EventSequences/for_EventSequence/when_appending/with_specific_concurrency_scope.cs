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

public class with_specific_concurrency_scope : given.an_event_sequence
{
    EventSourceId _eventSourceId;
    string _event;
    EventType _eventType;
    AppendRequest _command;
    JsonObject _eventContext;
    Identity _causedBy;
    ConcurrencyScope _scope;
    AppendResponse _response;

    void Establish()
    {
        _eventSourceId = Guid.NewGuid();
        _event = "Actual event";
        _eventType = new(Guid.NewGuid().ToString(), EventTypeGeneration.First);

        _eventContext = [];
        _eventSerializer.Serialize(_event).Returns(_eventContext);
        _causedBy = new("Subject", "Name", "UserName", new("BehalfOf_Subject", "BehalfOf_Name", "BehalfOf_UserName"));

        _scope = new(42L, _eventSourceId, "SomeEventStreamType", "SomeEventStream", "SomeEventSourceType", [_eventType]);

        _response = new()
        {
            CorrelationId = Guid.NewGuid(),
            SequenceNumber = 42,
            ConstraintViolations = [],
            Errors = []
        };

        _identityProvider.GetCurrent().Returns(_causedBy);

        _eventTypes.HasFor(typeof(string)).Returns(true);
        _eventTypes.GetEventTypeFor(typeof(string)).Returns(_eventType);
        _eventSequences
            .When(_ => _.Append(Arg.Any<AppendRequest>(), CallContext.Default))
            .Do(callInfo => _command = callInfo.Arg<AppendRequest>());
        _serviceAccessor.Services.EventSequences.Append(Arg.Any<AppendRequest>(), CallContext.Default).Returns(_response);
    }

    async Task Because() => await _eventSequence.Append(_eventSourceId, _event, concurrencyScope: _scope);

    [Fact] void should_append_event() => _command.ShouldNotBeNull();
    [Fact] void should_append_event_with_correct_event_source_id() => _command.EventSourceId.ShouldEqual(_eventSourceId.Value);
    [Fact] void should_append_event_with_correct_event_type() => _command.EventType.ToClient().ShouldEqual(_eventType);
    [Fact] void should_append_event_with_correct_event() => _command.Content.ShouldEqual(_eventContext.ToString());
    [Fact]
    void should_append_event_with_correct_concurrency_scope() => _command.ConcurrencyScope.ShouldMatch(scope =>
        scope.SequenceNumber == _scope.SequenceNumber.Value &&
        scope.EventSourceId &&
        scope.EventStreamType == _scope.EventStreamType &&
        scope.EventStreamId == _scope.EventStreamId &&
        scope.EventSourceType == _scope.EventSourceType &&
        scope.EventTypes.Count == _scope.EventTypes.Count() &&
        scope.EventTypes.Any(et => et.Id == _eventType.Id && et.Generation == _eventType.Generation));
}
