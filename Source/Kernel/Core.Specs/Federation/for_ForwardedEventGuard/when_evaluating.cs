// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Federation.for_ForwardedEventGuard;

public class when_evaluating : Specification
{
    static readonly EventStoreName _local = new("local");
    static readonly EventStoreName _remote = new("remote");
    static readonly EventStoreName _otherRemote = new("other-remote");

    ForwardedEventGuard _guard;
    ForwardedEventDecision _own;
    ForwardedEventDecision _first;
    ForwardedEventDecision _redelivered;
    ForwardedEventDecision _older;
    ForwardedEventDecision _next;
    ForwardedEventDecision _sameNumberOtherOrigin;

    void Establish() => _guard = new(_local);

    void Because()
    {
        _own = _guard.Evaluate(_local, new EventSequenceNumber(1));
        _first = _guard.Evaluate(_remote, new EventSequenceNumber(5));
        _redelivered = _guard.Evaluate(_remote, new EventSequenceNumber(5));
        _older = _guard.Evaluate(_remote, new EventSequenceNumber(3));
        _next = _guard.Evaluate(_remote, new EventSequenceNumber(6));
        _sameNumberOtherOrigin = _guard.Evaluate(_otherRemote, new EventSequenceNumber(5));
    }

    [Fact] void should_reject_an_event_that_originated_locally() => _own.ShouldEqual(ForwardedEventDecision.RejectOwnOrigin);
    [Fact] void should_accept_a_new_event() => _first.ShouldEqual(ForwardedEventDecision.Accept);
    [Fact] void should_drop_a_redelivered_event() => _redelivered.ShouldEqual(ForwardedEventDecision.Duplicate);
    [Fact] void should_drop_an_event_older_than_the_high_water_mark() => _older.ShouldEqual(ForwardedEventDecision.Duplicate);
    [Fact] void should_accept_the_next_event() => _next.ShouldEqual(ForwardedEventDecision.Accept);
    [Fact] void should_track_each_origin_separately() => _sameNumberOtherOrigin.ShouldEqual(ForwardedEventDecision.Accept);
}
