// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Contracts.EventSequences;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Identities;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class many_known_events : given.an_event_sequence
{
    EventSourceId _eventSourceId;
    List<string> _events;
    EventType _eventType;
    List<JsonObject> _eventContexts;
    IEnumerable<Causation> _causation;
    Identity _causedBy;
    AppendManyRequest _command;
    AppendManyResponse _response;
    AppendManyResult _result;

    void Establish()
    {
        _eventSourceId = Guid.NewGuid();
        _events = ["Event1", "Event2", "Event3"];
        _eventType = new(Guid.NewGuid().ToString(), EventTypeGeneration.First);
        _eventContexts = _events.ConvertAll(e => new JsonObject { ["value"] = e });
        for (var i = 0; i < _events.Count; i++)
        {
            _eventSerializer.Serialize(_events[i]).Returns(_eventContexts[i]);
        }
        _causation = [new Causation(DateTimeOffset.UtcNow, Guid.NewGuid().ToString(), new Dictionary<string, string> { { "key", "42" } })];
        _causedBy = new("Subject", "Name", "UserName", new("BehalfOf_Subject", "BehalfOf_Name", "BehalfOf_UserName"));
        _eventTypes.HasFor(typeof(string)).Returns(true);
        _eventTypes.GetEventTypeFor(typeof(string)).Returns(_eventType);
        _eventSequences
            .When(_ => _.AppendMany(Arg.Any<AppendManyRequest>(), CallContext.Default))
            .Do((CallInfo callInfo) => _command = callInfo.Arg<AppendManyRequest>());
        _causationManager.GetCurrentChain().Returns(_causation.ToImmutableList());
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

    async Task Because() => _result = await _eventSequence.AppendMany(_eventSourceId, _events);

    [Fact] void should_append_events() => _command.ShouldNotBeNull();
    [Fact] void should_append_events_with_correct_event_source_id() => _command.Events.All(e => e.EventSourceId == _eventSourceId).ShouldBeTrue();
    [Fact] void should_append_events_with_correct_event_type() => _command.Events.All(e => e.EventType.ToClient().Equals(_eventType)).ShouldBeTrue();
    [Fact] void should_append_events_with_correct_content() => _command.Events.Select(e => e.Content).ShouldEqual(_eventContexts.Select(c => c.ToString()));
    [Fact] void should_append_events_with_correct_causations() => _command.Causation.ToClient().ShouldEqual(_causation);
    [Fact] void should_append_events_with_correct_caused_by() => _command.CausedBy.ToClient().ShouldEqual(_causedBy);
    [Fact] void should_return_result_with_sequence_numbers() => _result.SequenceNumbers.Select(_ => _.Value).ShouldEqual(_response.SequenceNumbers);
}
