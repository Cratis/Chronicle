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

public class known_event : given.an_event_sequence
{
    EventSourceId _eventSourceId;
    string _event;
    EventType _eventType;
    AppendRequest _command;
    JsonObject _eventContext;
    IEnumerable<Causation> _causation;
    Identity _causedBy;
    AppendResponse _response;

    void Establish()
    {
        _eventSourceId = Guid.NewGuid();
        _event = "Actual event";
        _eventType = new(Guid.NewGuid().ToString(), EventTypeGeneration.First);

        _eventContext = [];
        _eventSerializer.Serialize(_event).Returns(_eventContext);

        _causation =
        [
            new Causation(DateTimeOffset.UtcNow, Guid.NewGuid().ToString(), new Dictionary<string, string> { { "key", "42" } })
        ];

        _causedBy = new("Subject", "Name", "UserName", new("BehalfOf_Subject", "BehalfOf_Name", "BehalfOf_UserName"));

        _eventTypes.HasFor(typeof(string)).Returns(true);
        _eventTypes.GetEventTypeFor(typeof(string)).Returns(_eventType);
        _eventSequences
            .When(_ => _.Append(Arg.Any<AppendRequest>(), CallContext.Default))
            .Do((CallInfo callInfo) => _command = callInfo.Arg<AppendRequest>());
        _causationManager.GetCurrentChain().Returns(_causation.ToImmutableList());
        _identityProvider.GetCurrent().Returns(_causedBy);

        _response = new()
        {
            CorrelationId = Guid.NewGuid(),
            SequenceNumber = 42,
            ConstraintViolations = [],
            Errors = []
        };

        _serviceAccessor.Services.EventSequences.Append(Arg.Any<AppendRequest>(), CallContext.Default).Returns(_response);
    }

    async Task Because() => await event_sequence.Append(_eventSourceId, _event);

    [Fact] void should_append_event() => _command.ShouldNotBeNull();
    [Fact] void should_append_event_with_correct_event_source_id() => _command.EventSourceId.ShouldEqual(_eventSourceId.Value);
    [Fact] void should_append_event_with_correct_event_type() => _command.EventType.ToClient().ShouldEqual(_eventType);
    [Fact] void should_append_event_with_correct_event() => _command.Content.ShouldEqual(_eventContext.ToString());
    [Fact] void should_append_event_with_correct_causations() => _command.Causation.ToClient().ShouldEqual(_causation);
    [Fact] void should_append_event_with_correct_caused_by() => _command.CausedBy.ToClient().ShouldEqual(_causedBy);
}
