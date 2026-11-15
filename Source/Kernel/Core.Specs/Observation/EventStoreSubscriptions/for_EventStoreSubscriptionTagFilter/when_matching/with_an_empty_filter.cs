// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionTagFilter.when_matching;

public class with_an_empty_filter : Specification
{
    bool _tagged;
    bool _untagged;

    void Because()
    {
        _tagged = EventStoreSubscriptionTagFilter.Matches([], [Tag("audience", "x")]);
        _untagged = EventStoreSubscriptionTagFilter.Matches([], []);
    }

    [Fact] void should_match_tagged_events() => _tagged.ShouldBeTrue();
    [Fact] void should_match_untagged_events() => _untagged.ShouldBeTrue();

    static NamedTag Tag(string name, string value) => new(new TagName(name), value);
}
