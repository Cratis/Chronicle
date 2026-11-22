// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Chronicle.Storage.Events.Constraints.V2;

public class when_advancing_authority_revision : Specification
{
    UniqueConstraintAuthorityRevision _first;
    UniqueConstraintAuthorityRevision _last;
    Result<UniqueConstraintAuthorityRevision, UniqueConstraintWriteFailure> _exhausted;

    void Because()
    {
        _first = new UniqueConstraintAuthorityRevision(0).Next().AsT0;
        _last = new UniqueConstraintAuthorityRevision(long.MaxValue - 1).Next().AsT0;
        _exhausted = _last.Next();
    }

    [Fact] void should_advance_zero_once() => _first.Value.ShouldEqual(1L);
    [Fact] void should_reach_the_last_revision_without_overflow() => _last.Value.ShouldEqual(long.MaxValue);
    [Fact] void should_refuse_exhaustion_instead_of_wrapping() => _exhausted.AsT1.ShouldEqual(UniqueConstraintWriteFailure.RevisionExhausted);
}
