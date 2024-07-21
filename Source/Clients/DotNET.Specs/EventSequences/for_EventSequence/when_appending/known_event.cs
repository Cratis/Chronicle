// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Contracts.EventSequences;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Identities;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class known_event : given.an_event_sequence
{
    EventSourceId event_source_id;
    string @event;
    EventType event_type;
    AppendRequest command;
    JsonObject event_content;
    IEnumerable<Causation> causation;
    Identity caused_by;


    void Establish()
    {
        event_source_id = Guid.NewGuid();
        @event = "Actual event";
        event_type = new(Guid.NewGuid().ToString(), EventGeneration.First);

        event_content = [];
        event_serializer.Setup(_ => _.Serialize(@event)).ReturnsAsync(event_content);

        causation =
        [
            new Causation(DateTimeOffset.UtcNow, Guid.NewGuid().ToString(), new Dictionary<string, string> { { "key", "42" } })
        ];

        caused_by = new("Subject", "Name", "UserName", new("BehalfOf_Subject", "BehalfOf_Name", "BehalfOf_UserName"));

        event_types.Setup(_ => _.HasFor(typeof(string))).Returns(true);
        event_types.Setup(_ => _.GetEventTypeFor(typeof(string))).Returns(event_type);
        event_sequences
            .Setup(_ => _.Append(IsAny<AppendRequest>()))
            .Callback((AppendRequest _) => command = _);
        causation_manager.Setup(_ => _.GetCurrentChain()).Returns(causation.ToImmutableList());
        identity_provider.Setup(_ => _.GetCurrent()).Returns(caused_by);
    }

    async Task Because() => await event_sequence.Append(event_source_id, @event);

    [Fact] void should_append_event() => command.ShouldNotBeNull();
    [Fact] void should_append_event_with_correct_event_source_id() => command.EventSourceId.ShouldEqual(event_source_id.Value);
    [Fact] void should_append_event_with_correct_event_type() => command.EventType.ToClient().ShouldEqual(event_type);
    [Fact] void should_append_event_with_correct_event() => command.Content.ShouldEqual(event_content.ToString());
    [Fact] void should_append_event_with_correct_causations() => command.Causation.ToClient().ShouldEqual(causation);
    [Fact] void should_append_event_with_correct_caused_by() => command.CausedBy.ToClient().ShouldEqual(caused_by);
}
