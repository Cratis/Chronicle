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

public class many_known_events_with_explicit_subject : given.an_event_sequence
{
    EventSourceId _eventSourceId;
    List<string> _events;
    EventType _eventType;
    IEnumerable<Causation> _causation;
    Identity _causedBy;
    ConcurrencyScope _scope;
    AppendManyRequest _command;
    AppendManyResponse _response;
    Subject _subject;

    void Establish()
    {
        _eventSourceId = Guid.NewGuid();
        _events = ["Event1", "Event2"];
        _eventType = new(Guid.NewGuid().ToString(), EventTypeGeneration.First);
        _subject = "explicit-subject";
        for (var i = 0; i < _events.Count; i++)
        {
            _eventSerializer.Serialize(_events[i]).Returns(new JsonObject { ["value"] = _events[i] });
        }
        _causation = [new Causation(DateTimeOffset.UtcNow, Guid.NewGuid().ToString(), new Dictionary<string, string> { { "key", "42" } })];
        _causedBy = new("Subject", "Name", "UserName", new("BehalfOf_Subject", "BehalfOf_Name", "BehalfOf_UserName"));
        _scope = new(42UL, _eventSourceId);
        _eventTypes.HasFor(typeof(string)).Returns(true);
        _eventTypes.GetEventTypeFor(typeof(string)).Returns(_eventType);
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

    async Task Because() => await _eventSequence.AppendMany(_eventSourceId, _events, subject: _subject);

    [Fact] void should_append_first_event_with_explicit_subject() => _command.Events[0].Subject.ShouldEqual(_subject.Value);
    [Fact] void should_append_second_event_with_explicit_subject() => _command.Events[1].Subject.ShouldEqual(_subject.Value);
}
