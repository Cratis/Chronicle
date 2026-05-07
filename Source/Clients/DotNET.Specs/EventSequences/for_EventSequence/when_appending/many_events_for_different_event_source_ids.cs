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

public class many_events_for_different_event_source_ids : given.an_event_sequence
{
    List<EventForEventSourceId> _events;
    EventType _eventType;
    JsonObject _eventContext;
    Causation _eventCausation;
    IEnumerable<Causation> _causation;
    Identity _causedBy;
    AppendManyRequest _command;
    AppendManyResponse _response;
    AppendManyResult _result;
    Dictionary<EventSourceId, ConcurrencyScope> _concurrencyScopes;

    void Establish()
    {
        _eventType = new(Guid.NewGuid().ToString(), EventTypeGeneration.First);
        _eventContext = [];
        _eventSerializer.Serialize(Arg.Any<string>()).Returns(_eventContext);

        var causation1 = new Causation(DateTimeOffset.UtcNow, "type1", new Dictionary<string, string> { { "key", "1" } });
        var causation2 = new Causation(DateTimeOffset.UtcNow, "type2", new Dictionary<string, string> { { "key", "2" } });
        _eventCausation = causation1;

        _events =
        [
            new EventForEventSourceId(Guid.NewGuid(), "Event1", causation1),
            new EventForEventSourceId(Guid.NewGuid(), "Event2", causation2),
            new EventForEventSourceId(Guid.NewGuid(), "Event3", causation1)
        ];

        _causation =
        [
            new Causation(DateTimeOffset.UtcNow, Guid.NewGuid().ToString(), new Dictionary<string, string> { { "key", "42" } })
        ];

        _concurrencyScopes = new()
        {
            { _events[0].EventSourceId, new ConcurrencyScope(41UL, _events[0].EventSourceId) },
            { _events[1].EventSourceId, new ConcurrencyScope(42UL, _events[1].EventSourceId) },
            { _events[2].EventSourceId, new ConcurrencyScope(43UL, _events[2].EventSourceId) }
        };

        _causedBy = new("Subject", "Name", "UserName", new("BehalfOf_Subject", "BehalfOf_Name", "BehalfOf_UserName"));

        _eventTypes.HasFor(typeof(string)).Returns(true);
        _eventTypes.GetEventTypeFor(typeof(string)).Returns(_eventType);
        _eventSequences
            .When(_ => _.AppendMany(Arg.Any<AppendManyRequest>(), CallContext.Default))
            .Do(callInfo => _command = callInfo.Arg<AppendManyRequest>());
        _causationManager.GetCurrentChain().Returns(_causation.ToImmutableList());
        _concurrencyScopeStrategy.GetScope(_events[0].EventSourceId, _events[0].EventStreamType, _events[0].EventStreamId, _events[0].EventSourceType, default).Returns(Task.FromResult(_concurrencyScopes[_events[0].EventSourceId]));
        _concurrencyScopeStrategy.GetScope(_events[1].EventSourceId, _events[1].EventStreamType, _events[1].EventStreamId, _events[1].EventSourceType, default).Returns(Task.FromResult(_concurrencyScopes[_events[1].EventSourceId]));
        _concurrencyScopeStrategy.GetScope(_events[2].EventSourceId, _events[2].EventStreamType, _events[2].EventStreamId, _events[2].EventSourceType, default).Returns(Task.FromResult(_concurrencyScopes[_events[2].EventSourceId]));
        _identityProvider.GetCurrent().Returns(_causedBy);

        _response = new()
        {
            CorrelationId = Guid.NewGuid(),
            SequenceNumbers = [42, 43, 44],
            ConstraintViolations = [],
            Errors = []
        };

        _serviceAccessor.Services.EventSequences.AppendMany(Arg.Any<AppendManyRequest>(), CallContext.Default).Returns(_response);
    }

    async Task Because() => _result = await _eventSequence.AppendMany(_events);

    [Fact] void should_append_events() => _command.ShouldNotBeNull();
    [Fact] void should_append_correct_number_of_events() => _command.Events.Count.ShouldEqual(_events.Count);
    [Fact] void should_append_events_with_correct_event_source_ids() => _command.Events.Select(e => (EventSourceId)e.EventSourceId).ShouldEqual(_events.Select(e => e.EventSourceId));
    [Fact] void should_append_events_with_correct_event_type() => _command.Events.All(e => e.EventType.ToClient().Equals(_eventType)).ShouldBeTrue();
    [Fact] void should_append_events_with_correct_causations() => _command.Causation.ToClient().ShouldEqual([_eventCausation]);
    [Fact] void should_append_events_with_correct_caused_by() => _command.CausedBy.ToClient().ShouldEqual(_causedBy);
    [Fact] void should_append_events_with_strategy_concurrency_scopes() => _command.ConcurrencyScopes.Count.ShouldEqual(_concurrencyScopes.Count);
    [Fact] void should_append_first_event_with_strategy_concurrency_scope() => _command.ConcurrencyScopes[_events[0].EventSourceId.Value].SequenceNumber.ShouldEqual((ulong)_concurrencyScopes[_events[0].EventSourceId].SequenceNumber);
    [Fact] void should_append_second_event_with_strategy_concurrency_scope() => _command.ConcurrencyScopes[_events[1].EventSourceId.Value].SequenceNumber.ShouldEqual((ulong)_concurrencyScopes[_events[1].EventSourceId].SequenceNumber);
    [Fact] void should_append_third_event_with_strategy_concurrency_scope() => _command.ConcurrencyScopes[_events[2].EventSourceId.Value].SequenceNumber.ShouldEqual((ulong)_concurrencyScopes[_events[2].EventSourceId].SequenceNumber);
    [Fact] void should_return_result_with_sequence_numbers() => _result.SequenceNumbers.Select(_ => _.Value).ShouldEqual(_response.SequenceNumbers);
}
