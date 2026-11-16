// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionEventContext;

public class when_adding_a_pending_future_save : given.a_context
{
    ProjectionFutureId _futureId = Guid.NewGuid();
    ProjectionEventContext _copy;

    void Establish() => _copy = _context with { NeedsInitialState = true };

    void Because()
    {
        _context.AddPendingFutureSave(_context.Key, _context.Changeset);
        _context.AddPendingFutureSave(_context.Key, _context.Changeset, _futureId);
    }

    [Fact] void should_have_pending_saves() => _context.HasPendingFutureSaves.ShouldBeTrue();
    [Fact] void should_not_invent_an_id_for_an_unstored_future() => _context.PendingFutureSaves.First().FutureId.ShouldBeNull();
    [Fact] void should_keep_the_stored_future_id() => _context.PendingFutureSaves.Last().FutureId.ShouldEqual(_futureId);
    [Fact] void should_keep_the_changeset() => _context.PendingFutureSaves.First().Changeset.ShouldEqual(_context.Changeset);
    [Fact] void should_share_pending_saves_with_record_copies() => _copy.PendingFutureSaves.Count().ShouldEqual(2);
}
