// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Occurrences;

namespace Cratis.Chronicle.Testing.Occurrences.for_OccurrenceContextBuilder.when_building;

public class with_an_explicit_id_and_an_idempotency_id : Specification
{
    OccurrenceContext _explicitFirst;
    OccurrenceContext _explicitLast;

    void Because()
    {
        _explicitFirst = new OccurrenceContextBuilder().WithId(new("explicit")).WithIdempotencyId("one").BuildDetached();
        _explicitLast = new OccurrenceContextBuilder().WithIdempotencyId("one").WithId(new("explicit")).BuildDetached();
    }

    [Fact] void should_use_the_explicit_id_when_it_is_set_first() => _explicitFirst.Id.Value.ShouldEqual("explicit");
    [Fact] void should_use_the_explicit_id_when_it_is_set_last() => _explicitLast.Id.Value.ShouldEqual("explicit");
}
