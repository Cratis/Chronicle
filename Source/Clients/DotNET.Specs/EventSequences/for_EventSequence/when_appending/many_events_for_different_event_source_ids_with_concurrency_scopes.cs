// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Contracts.EventSequences;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Identities;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class many_events_for_different_event_source_ids_with_concurrency_scopes : given.an_event_sequence
{
    List<EventForEventSourceId> _events;
    Dictionary<EventSourceId, ConcurrencyScope> _concurrencyScopes;
    EventType _eventType;
    JsonObject _eventContext;
    IEnumerable<Causation> _causation;
    Identity _causedBy;
    AppendManyRequest _command;
    AppendManyResponse _response;

    void Establish()
    {
        _eventType = new(Guid.NewGuid().ToString(), EventTypeGeneration.First);
        _eventContext = [];
        _eventSerializer.Serialize(Arg.Any<string>()).Returns(_eventContext);

        var causation = new Causation(DateTimeOffset.UtcNow, "type", new Dictionary<string, string>());
        var eventSourceId1 = Guid.NewGuid();
        var eventSourceId2 = Guid.NewGuid();

        _events =
        [
            new EventForEventSourceId(eventSourceId1, "Event1", causation),
            new EventForEventSourceId(eventSourceId2, "Event2", causation)
        ];

        _concurrencyScopes = new Dictionary<EventSourceId, ConcurrencyScope>
        {
            { eventSourceId1, new ConcurrencyScope(42UL) },
            { eventSourceId2, new ConcurrencyScope(43UL) }
        };

        _causation =
        [
            new Causation(DateTimeOffset.UtcNow, Guid.NewGuid().ToString(), new Dictionary<string, string>())
        ];

        _causedBy = new("Subject", "Name", "UserName", new("BehalfOf_Subject", "BehalfOf_Name", "BehalfOf_UserName"));

        _eventTypes.HasFor(typeof(string)).Returns(true);
        _eventTypes.GetEventTypeFor(typeof(string)).Returns(_eventType);
        _eventSequences
            .When(_ => _.AppendMany(Arg.Any<AppendManyRequest>(), CallContext.Default))
            .Do(callInfo => _command = callInfo.Arg<AppendManyRequest>());
        _causationManager.GetCurrentChain().Returns(_causation.ToImmutableList());
        _identityProvider.GetCurrent().Returns(_causedBy);

        _response = new()
        {
            CorrelationId = Guid.NewGuid(),
            SequenceNumbers = [42, 43],
            ConstraintViolations = [],
            Errors = []
        };

        _serviceAccessor.Services.EventSequences.AppendMany(Arg.Any<AppendManyRequest>(), CallContext.Default).Returns(_response);
    }

    async Task Because() => await _eventSequence.AppendMany(_events, concurrencyScopes: _concurrencyScopes);

    [Fact] void should_pass_concurrency_scopes() => _command.ConcurrencyScopes.Count.ShouldEqual(_concurrencyScopes.Count);
    [Fact] void should_pass_first_concurrency_scope() => _command.ConcurrencyScopes[_events[0].EventSourceId.Value].SequenceNumber.ShouldEqual((ulong)_concurrencyScopes[_events[0].EventSourceId].SequenceNumber);
    [Fact] void should_pass_second_concurrency_scope() => _command.ConcurrencyScopes[_events[1].EventSourceId.Value].SequenceNumber.ShouldEqual((ulong)_concurrencyScopes[_events[1].EventSourceId].SequenceNumber);
}
