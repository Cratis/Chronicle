// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Specs.Tagging.given;

public class an_event_tag_resolver : Specification
{
    protected IEventTagEnricher _enricher;
    protected EventTagResolver _resolver;
    protected EventSequenceId _sequence = EventSequenceId.Log;
    protected NamedTag[] _existing = [];

    void Establish()
    {
        _enricher = Substitute.For<IEventTagEnricher>();
        _enricher.GetTags(Arg.Any<EventTagContext>()).Returns([]);
        _resolver = new EventTagResolver([_enricher]);
    }

    protected IReadOnlyList<NamedTag> Resolve(object @event) => _resolver.ResolveTags(Context(@event));

    protected EventTagContext Context(object @event) => new(
        new EventStoreName("Operator"),
        _sequence,
        new EventSourceId("source"),
        new EventType("event", 1),
        @event,
        _existing);

    protected static NamedTag Tag(string name, string value) => new(new TagName(name), value);

    protected record PlainEvent(string Name);

    protected record InvitationSent([property: Audience] string Lobby, string Email);

    protected record InvitationSentTo([property: Audience] LobbyId Lobby);

    protected record LobbyId(string Value) : ConceptAs<string>(Value);

    [Audience]
    protected record RequiredAudienceEvent(string Name);
}
