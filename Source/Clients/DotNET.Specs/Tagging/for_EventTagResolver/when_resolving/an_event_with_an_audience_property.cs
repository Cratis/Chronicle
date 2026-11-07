// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Specs.Tagging.for_EventTagResolver.when_resolving;

public class an_event_with_an_audience_property : given.an_event_tag_resolver
{
    IReadOnlyList<NamedTag> _result;
    IReadOnlyList<NamedTag> _conceptResult;

    void Because()
    {
        _result = Resolve(new InvitationSent("lobby-a", "someone@example.com"));
        _conceptResult = Resolve(new InvitationSentTo(new LobbyId("lobby-b")));
    }

    [Fact] void should_stamp_the_property_value_as_the_audience() => _result.Single().ShouldEqual(Tag(WellKnownTags.Audience, "lobby-a"));
    [Fact] void should_stamp_the_underlying_value_of_a_concept() => _conceptResult.Single().ShouldEqual(Tag(WellKnownTags.Audience, "lobby-b"));
}
