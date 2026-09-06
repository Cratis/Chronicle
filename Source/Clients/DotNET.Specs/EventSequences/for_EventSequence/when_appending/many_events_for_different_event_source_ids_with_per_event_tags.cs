// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Identities;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class many_events_for_different_event_source_ids_with_per_event_tags : given.an_event_sequence
{
    List<EventForEventSourceId> _events;
    EventType _eventType;
    JsonObject _eventContext;
    Contracts.Sequences.AppendManyForEventSourcesRequest _command;
    Contracts.Sequences.AppendManyResponse _response;

    void Establish()
    {
        _eventType = new(Guid.NewGuid().ToString(), EventTypeGeneration.First);
        _eventContext = [];
        _eventSerializer.Serialize(Arg.Any<string>()).Returns(_eventContext);

        _events =
        [
            new EventForEventSourceId(Guid.NewGuid(), "Event1") { Tags = ["first", "shared"] },
            new EventForEventSourceId(Guid.NewGuid(), "Event2") { Tags = ["second"] }
        ];

        _eventTypes.HasFor(typeof(string)).Returns(true);
        _eventTypes.GetEventTypeFor(typeof(string)).Returns(_eventType);
        _sequences
            .When(_ => _.AppendManyForEventSources(Arg.Any<Contracts.Sequences.AppendManyForEventSourcesRequest>(), CallContext.Default))
            .Do(callInfo => _command = callInfo.Arg<Contracts.Sequences.AppendManyForEventSourcesRequest>());
        _causationManager.GetCurrentChain().Returns(ImmutableList<Causation>.Empty);
        _identityProvider.GetCurrent().Returns(Identity.NotSet);

        _response = new()
        {
            CorrelationId = Guid.NewGuid(),
            SequenceNumbers = [42, 43],
            ConstraintViolations = [],
            Errors = [],
            ConcurrencyViolations = []
        };

        _sequences.AppendManyForEventSources(Arg.Any<Contracts.Sequences.AppendManyForEventSourcesRequest>(), CallContext.Default)
            .Returns(CommandResult<Contracts.Sequences.AppendManyResponse>.Success(Guid.NewGuid(), _response));
    }

    async Task Because() => await _eventSequence.AppendMany(_events);

    [Fact] void should_apply_the_first_events_tags() => _command.Events.ElementAt(0).Tags.ShouldContainOnly(["first", "shared"]);
    [Fact] void should_apply_the_second_events_tags() => _command.Events.Last().Tags.ShouldContainOnly(["second"]);
    [Fact] void should_not_bleed_tags_across_events() => _command.Events.Last().Tags.ShouldNotContain("first");
}
