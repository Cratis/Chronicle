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

public class many_events_for_different_event_source_ids_with_subjects : given.an_event_sequence
{
    List<EventForEventSourceId> _events;
    EventType _eventType;
    IEnumerable<Causation> _causation;
    Identity _causedBy;
    AppendManyRequest _command;
    AppendManyResponse _response;
    Dictionary<EventSourceId, ConcurrencyScope> _concurrencyScopes;

    void Establish()
    {
        _eventType = new(Guid.NewGuid().ToString(), EventTypeGeneration.First);

        var firstEvent = new EventWithSubject("derived-person", "First");
        var secondEvent = new EventWithSubject("ignored-person", "Second");
        _events =
        [
            new EventForEventSourceId(Guid.NewGuid(), firstEvent, Causation.Unknown()),
            new EventForEventSourceId(Guid.NewGuid(), secondEvent, Causation.Unknown()) { Subject = "explicit-person" }
        ];

        foreach (var @event in _events.Select(_ => (EventWithSubject)_.Event))
        {
            _eventSerializer.Serialize(@event).Returns(new JsonObject
            {
                ["personId"] = @event.PersonId,
                ["value"] = @event.Value
            });
        }

        _causation =
        [
            new Causation(DateTimeOffset.UtcNow, Guid.NewGuid().ToString(), new Dictionary<string, string> { { "key", "42" } })
        ];

        _concurrencyScopes = new()
        {
            { _events[0].EventSourceId, new ConcurrencyScope(41UL, _events[0].EventSourceId) },
            { _events[1].EventSourceId, new ConcurrencyScope(42UL, _events[1].EventSourceId) }
        };

        _causedBy = new("Subject", "Name", "UserName", new("BehalfOf_Subject", "BehalfOf_Name", "BehalfOf_UserName"));

        _eventTypes.HasFor(typeof(EventWithSubject)).Returns(true);
        _eventTypes.GetEventTypeFor(typeof(EventWithSubject)).Returns(_eventType);
        _eventSequences
            .When(_ => _.AppendMany(Arg.Any<AppendManyRequest>(), CallContext.Default))
            .Do(callInfo => _command = callInfo.Arg<AppendManyRequest>());
        _causationManager.GetCurrentChain().Returns(_causation.ToImmutableList());
        _concurrencyScopeStrategy.GetScope(_events[0].EventSourceId, _events[0].EventStreamType, _events[0].EventStreamId, _events[0].EventSourceType, default).Returns(Task.FromResult(_concurrencyScopes[_events[0].EventSourceId]));
        _concurrencyScopeStrategy.GetScope(_events[1].EventSourceId, _events[1].EventStreamType, _events[1].EventStreamId, _events[1].EventSourceType, default).Returns(Task.FromResult(_concurrencyScopes[_events[1].EventSourceId]));
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

    async Task Because() => await _eventSequence.AppendMany(_events);

    [Fact] void should_append_events() => _command.ShouldNotBeNull();
    [Fact] void should_append_first_event_with_resolved_subject() => _command.Events[0].Subject.ShouldEqual("derived-person");
    [Fact] void should_append_second_event_with_explicit_subject() => _command.Events[1].Subject.ShouldEqual("explicit-person");

    record EventWithSubject([Subject] string PersonId, string Value);
}
