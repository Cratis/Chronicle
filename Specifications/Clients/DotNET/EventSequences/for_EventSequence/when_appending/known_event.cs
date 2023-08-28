// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Aksio.Cratis.Auditing;
using Aksio.Cratis.Identities;

namespace Aksio.Cratis.EventSequences.for_EventSequence.when_appending;

public class known_event : given.an_event_sequence
{
    EventSourceId event_source_id;
    string @event;
    EventType event_type;
    AppendEvent command;
    dynamic metadata;
    JsonObject event_content;
    IEnumerable<Causation> causation;
    Identity caused_by;

    void Establish()
    {
        event_source_id = Guid.NewGuid();
        @event = "Actual event";
        event_type = new(Guid.NewGuid(), EventGeneration.First);

        event_content = new JsonObject();
        event_serializer.Setup(_ => _.Serialize(@event)).ReturnsAsync(event_content);

        causation = new[]
        {
            new Causation(DateTimeOffset.UtcNow, Guid.NewGuid().ToString(), new Dictionary<string, string> { { "key", "42" } })
        };

        caused_by = new("Subject", "Name", "UserName", new("BehalfOf_Subject", "BehalfOf_Name", "BehalfOf_UserName"));

        event_types.Setup(_ => _.HasFor(typeof(string))).Returns(true);
        event_types.Setup(_ => _.GetEventTypeFor(typeof(string))).Returns(event_type);
        connection
            .Setup(_ => _.PerformCommand(IsAny<string>(), IsAny<object>(), IsAny<object>()))
            .Callback((string _, object command, object metadata) =>
            {
                this.command = command as AppendEvent;
                this.metadata = metadata;
            });
        causation_manager.Setup(_ => _.GetCurrentChain()).Returns(causation.ToImmutableList());
        identity_provider.Setup(_ => _.GetCurrent()).Returns(caused_by);
    }

    async Task Because() => await event_sequence.Append(event_source_id, @event);

    [Fact] void should_append_event() => command.ShouldNotBeNull();
    [Fact] void should_append_event_with_correct_event_source_id() => command.EventSourceId.ShouldEqual(event_source_id);
    [Fact] void should_append_event_with_correct_event_type() => command.EventType.ShouldEqual(event_type);
    [Fact] void should_append_event_with_correct_event() => command.Content.ShouldEqual(event_content);
    [Fact] void should_append_event_with_correct_causations() => command.Causation.ShouldEqual(causation);
    [Fact] void should_append_event_with_correct_caused_by() => command.CausedBy.ShouldEqual(caused_by);
}
