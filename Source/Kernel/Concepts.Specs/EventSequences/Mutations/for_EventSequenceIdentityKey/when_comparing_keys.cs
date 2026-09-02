// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Chronicle.Concepts.EventSequences.Mutations.for_EventSequenceIdentityKey;

public class when_comparing_keys : Specification
{
    EventSequenceIdentityKey _first;
    EventSequenceIdentityKey _same;
    EventSequenceIdentityKey _different;
    EventSequenceIdentityKey _empty;
    EventSequenceIdentityKey _uninitialized;

    void Establish()
    {
        _first = new(Encoding.UTF8.GetBytes("event-log"));
        _same = new(Encoding.UTF8.GetBytes("event-log"));
        _different = new(Encoding.UTF8.GetBytes("Event-Log"));
        _empty = new([]);
        _uninitialized = default;
    }

    [Fact] void should_compare_equal_content_as_equal() => (_first == _same).ShouldBeTrue();
    [Fact] void should_give_equal_content_the_same_stable_hash() => _first.GetHashCode().ShouldEqual(_same.GetHashCode());
    [Fact] void should_distinguish_different_content() => (_first != _different).ShouldBeTrue();
    [Fact] void should_initialize_an_explicit_empty_key() => _empty.IsInitialized.ShouldBeTrue();
    [Fact] void should_leave_a_default_key_uninitialized() => _uninitialized.IsInitialized.ShouldBeFalse();
    [Fact] void should_distinguish_default_from_explicit_empty() => (_uninitialized != _empty).ShouldBeTrue();
}
