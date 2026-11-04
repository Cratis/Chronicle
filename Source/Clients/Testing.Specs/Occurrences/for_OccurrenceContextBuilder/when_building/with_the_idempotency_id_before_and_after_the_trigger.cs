// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Occurrences;

namespace Cratis.Chronicle.Testing.Occurrences.for_OccurrenceContextBuilder.when_building;

public class with_the_idempotency_id_before_and_after_the_trigger : Specification
{
    OccurrenceContext _before;
    OccurrenceContext _after;

    void Because()
    {
        _before = new OccurrenceContextBuilder().WithIdempotencyId("one").ForTrigger(new("detached")).BuildDetached();
        _after = new OccurrenceContextBuilder().ForTrigger(new("detached")).WithIdempotencyId("one").BuildDetached();
    }

    [Fact] void should_derive_the_same_identity() => _before.Id.ShouldEqual(_after.Id);
}
