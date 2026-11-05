// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.Constraints.for_ClosedStream;

public class when_marking_for_review : Specification
{
    ClosedStream _original;
    ClosedStream _pending;

    void Establish() => _original = new(new(EventSourceId: "source"), "owner", 1, null);

    void Because() => _pending = _original with { PendingReview = true };

    [Fact] void should_default_to_not_requiring_review() => _original.PendingReview.ShouldBeFalse();
    [Fact] void should_include_the_flag_in_value_equality() => _pending.Equals(_original).ShouldBeFalse();
    [Fact] void should_preserve_the_owner_and_scope_identity() => (_pending.Owner, _pending.Scope).ShouldEqual((_original.Owner, _original.Scope));
}
