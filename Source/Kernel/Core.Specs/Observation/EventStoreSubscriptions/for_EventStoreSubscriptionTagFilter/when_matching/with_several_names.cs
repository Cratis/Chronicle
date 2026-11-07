// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionTagFilter.when_matching;

public class with_several_names : Specification
{
    NamedTag[] _filter = [new(new TagName("audience"), "a"), new(new TagName("region"), "eu")];
    bool _both;
    bool _onlyAudience;

    void Because()
    {
        _both = EventStoreSubscriptionTagFilter.Matches(_filter, [new(new TagName("region"), "eu"), new(new TagName("audience"), "a")]);
        _onlyAudience = EventStoreSubscriptionTagFilter.Matches(_filter, [new(new TagName("audience"), "a")]);
    }

    [Fact] void should_match_when_every_name_matches() => _both.ShouldBeTrue();
    [Fact] void should_not_match_when_a_name_is_missing() => _onlyAudience.ShouldBeFalse();
}
