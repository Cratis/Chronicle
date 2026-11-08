// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Federation.for_FederationEventSelector;

public class when_selecting : Specification
{
    FederationEventSelector _selector;
    FederationEventSelector _tagFiltered;
    FederationEventSelector _none;

    void Establish()
    {
        _selector = new([new(["b", "a"], []), new(["c", "a"], [])]);
        _tagFiltered = new([new(["a"], [new NamedTag("region", "eu")])]);
        _none = new([]);
    }

    [Fact] void should_request_the_sorted_union_of_event_types() => string.Join(',', _selector.EventTypeIds).ShouldEqual("a,b,c");
    [Fact] void should_want_a_subscribed_type() => _selector.Wants("c").ShouldBeTrue();
    [Fact] void should_want_a_subscribed_type_with_generation() => _selector.Wants("c+2").ShouldBeTrue();
    [Fact] void should_not_want_another_type() => _selector.Wants("z").ShouldBeFalse();
    [Fact] void should_not_forward_when_a_named_tag_filter_cannot_be_satisfied_by_the_wire() => _tagFiltered.Wants("a").ShouldBeFalse();
    [Fact] void should_request_nothing_without_subscriptions() => _none.EventTypeIds.ShouldBeEmpty();
}
