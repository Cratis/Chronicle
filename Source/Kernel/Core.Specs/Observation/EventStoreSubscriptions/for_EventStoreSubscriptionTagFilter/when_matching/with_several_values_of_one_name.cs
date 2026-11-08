// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionTagFilter.when_matching;

public class with_several_values_of_one_name : Specification
{
    NamedTag[] _filter = [new(new TagName("audience"), "a"), new(new TagName("audience"), "b")];
    bool _first;
    bool _second;
    bool _third;

    void Because()
    {
        _first = EventStoreSubscriptionTagFilter.Matches(_filter, [new(new TagName("audience"), "a")]);
        _second = EventStoreSubscriptionTagFilter.Matches(_filter, [new(new TagName("audience"), "b")]);
        _third = EventStoreSubscriptionTagFilter.Matches(_filter, [new(new TagName("audience"), "c")]);
    }

    [Fact] void should_match_the_first_alternative() => _first.ShouldBeTrue();
    [Fact] void should_match_the_second_alternative() => _second.ShouldBeTrue();
    [Fact] void should_not_match_other_values() => _third.ShouldBeFalse();
}
