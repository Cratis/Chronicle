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

public class many_known_events_with_subjects : given.an_event_sequence
{
    EventSourceId _eventSourceId;
    List<EventWithSubject> _events;
    EventType _eventType;
    IEnumerable<Causation> _causation;
    Identity _causedBy;
    ConcurrencyScope _scope;
    AppendManyRequest _command;
    AppendManyResponse _response;

    void Establish()
    {
        _eventSourceId = Guid.NewGuid();
        _events =
        [
            new("person-1", "First"),
            new("person-2", "Second")
        ];
        _eventType = new(Guid.NewGuid().ToString(), EventTypeGeneration.First);

        foreach (var @event in _events)
        {
            _eventSerializer.Serialize(@event).Returns(new JsonObject
            {
                ["personId"] = @event.PersonId,
                ["value"] = @event.Value
            });
        }

        _causation = [new Causation(DateTimeOffset.UtcNow, Guid.NewGuid().ToString(), new Dictionary<string, string> { { "key", "42" } })];
        _causedBy = new("Subject", "Name", "UserName", new("BehalfOf_Subject", "BehalfOf_Name", "BehalfOf_UserName"));
        _scope = new(42UL, _eventSourceId);
        _eventTypes.HasFor(typeof(EventWithSubject)).Returns(true);
        _eventTypes.GetEventTypeFor(typeof(EventWithSubject)).Returns(_eventType);
        _eventSequences
            .When(_ => _.AppendMany(Arg.Any<AppendManyRequest>(), CallContext.Default))
            .Do(callInfo => _command = callInfo.Arg<AppendManyRequest>());
        _causationManager.GetCurrentChain().Returns(_causation.ToImmutableList());
        _concurrencyScopeStrategy.GetScope(_eventSourceId, EventStreamType.All, EventStreamId.Default, EventSourceType.Default, default).Returns(Task.FromResult(_scope));
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

    async Task Because() => await _eventSequence.AppendMany(_eventSourceId, _events);

    [Fact] void should_append_events() => _command.ShouldNotBeNull();
    [Fact] void should_append_first_event_with_resolved_subject() => _command.Events[0].Subject.ShouldEqual(_events[0].PersonId);
    [Fact] void should_append_second_event_with_resolved_subject() => _command.Events[1].Subject.ShouldEqual(_events[1].PersonId);

    record EventWithSubject([Subject] string PersonId, string Value);
}
