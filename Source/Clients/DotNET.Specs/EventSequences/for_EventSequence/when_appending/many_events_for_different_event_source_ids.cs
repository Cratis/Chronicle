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

public class many_events_for_different_event_source_ids : given.an_event_sequence
{
    List<EventForEventSourceId> _events;
    EventType _eventType;
    JsonObject _eventContext;
    IEnumerable<Causation> _causation;
    Identity _causedBy;
    AppendManyRequest _command;
    AppendManyResponse _response;
    AppendManyResult _result;

    void Establish()
    {
        _eventType = new(Guid.NewGuid().ToString(), EventTypeGeneration.First);
        _eventContext = [];
        _eventSerializer.Serialize(Arg.Any<string>()).Returns(_eventContext);

        var causation1 = new Causation(DateTimeOffset.UtcNow, "type1", new Dictionary<string, string> { { "key", "1" } });
        var causation2 = new Causation(DateTimeOffset.UtcNow, "type2", new Dictionary<string, string> { { "key", "2" } });

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
    [Fact] void should_append_events_with_correct_causations() => _command.Causation.ToClient().ShouldEqual(_causation);
    [Fact] void should_append_events_with_correct_caused_by() => _command.CausedBy.ToClient().ShouldEqual(_causedBy);
    [Fact] void should_return_result_with_sequence_numbers() => _result.SequenceNumbers.Select(_ => _.Value).ShouldEqual(_response.SequenceNumbers);
}
