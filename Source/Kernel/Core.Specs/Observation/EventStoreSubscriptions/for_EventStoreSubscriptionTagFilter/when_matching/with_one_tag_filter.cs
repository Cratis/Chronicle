// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionTagFilter.when_matching;

public class with_one_tag_filter : Specification
{
    NamedTag[] _filter = [new(new TagName("audience"), "lobby-a")];
    bool _matching;
    bool _otherValue;
    bool _untagged;
    bool _otherName;

    void Because()
    {
        _matching = EventStoreSubscriptionTagFilter.Matches(_filter, [new(new TagName("audience"), "lobby-a")]);
        _otherValue = EventStoreSubscriptionTagFilter.Matches(_filter, [new(new TagName("audience"), "lobby-b")]);
        _untagged = EventStoreSubscriptionTagFilter.Matches(_filter, []);
        _otherName = EventStoreSubscriptionTagFilter.Matches(_filter, [new(new TagName("from"), "lobby-a")]);
    }

    [Fact] void should_match_the_event_with_the_tag() => _matching.ShouldBeTrue();
    [Fact] void should_not_match_another_value() => _otherValue.ShouldBeFalse();
    [Fact] void should_never_match_untagged_events() => _untagged.ShouldBeFalse();
    [Fact] void should_not_match_the_value_under_another_name() => _otherName.ShouldBeFalse();
}
