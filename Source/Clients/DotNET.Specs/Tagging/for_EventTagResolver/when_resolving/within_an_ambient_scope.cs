// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Specs.Tagging.for_EventTagResolver.when_resolving;

public class within_an_ambient_scope : given.an_event_tag_resolver
{
    IReadOnlyList<NamedTag> _outboxAudience;
    IReadOnlyList<NamedTag> _logAudienceOnUnmarkedType;
    IReadOnlyList<NamedTag> _logAudienceOnMarkedType;
    IReadOnlyList<NamedTag> _logScope;
    IReadOnlyList<NamedTag> _afterDisposal;
    IReadOnlyList<NamedTag> _twoAudiences;

    void Because()
    {
        using (EventTags.Audience("lobby-a"))
        {
            _sequence = EventSequenceId.Outbox;
            _outboxAudience = Resolve(new PlainEvent("x"));

            _sequence = EventSequenceId.Log;
            _logAudienceOnUnmarkedType = Resolve(new PlainEvent("x"));
            _logAudienceOnMarkedType = Resolve(new RequiredAudienceEvent("x"));

            using (EventTags.Scope("region", "eu"))
            {
                _logScope = Resolve(new PlainEvent("x"));
            }

            _afterDisposal = Resolve(new PlainEvent("x"));
        }

        using (EventTags.Audience("lobby-a"))
        using (EventTags.Audience("lobby-b"))
        {
            _sequence = EventSequenceId.Outbox;
            _twoAudiences = Resolve(new PlainEvent("x"));
        }
    }

    [Fact] void should_stamp_the_audience_on_the_outbox() => _outboxAudience.Single().ShouldEqual(Tag(WellKnownTags.Audience, "lobby-a"));
    [Fact] void should_not_stamp_the_audience_on_other_sequences_for_unmarked_types() => _logAudienceOnUnmarkedType.ShouldBeEmpty();
    [Fact] void should_stamp_the_audience_on_any_sequence_for_marked_types() => _logAudienceOnMarkedType.Single().ShouldEqual(Tag(WellKnownTags.Audience, "lobby-a"));
    [Fact] void should_stamp_other_scoped_tags_on_every_sequence() => _logScope.Single().ShouldEqual(Tag("region", "eu"));
    [Fact] void should_stop_stamping_when_the_scope_is_disposed() => _afterDisposal.ShouldBeEmpty();
    [Fact] void should_address_every_audience_of_nested_scopes() => _twoAudiences.Select(_ => _.Value).ShouldContainOnly("lobby-a", "lobby-b");
}
