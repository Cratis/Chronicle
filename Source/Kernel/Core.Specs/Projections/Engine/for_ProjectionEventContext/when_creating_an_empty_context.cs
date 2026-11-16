// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionEventContext;

public class when_creating_an_empty_context : given.a_context
{
    [Fact] void should_have_an_undefined_key() => _context.Key.ShouldEqual(Key.Undefined);
    [Fact] void should_keep_the_event_type() => _context.EventType.ShouldEqual(new EventType("event", 1));
    [Fact] void should_keep_the_sequence_number() => _context.EventSequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
    [Fact] void should_have_no_changes() => _context.Changeset.HasChanges.ShouldBeFalse();
    [Fact] void should_have_no_futures() => _context.DeferredFutures.ShouldBeEmpty();
    [Fact] void should_have_no_pending_saves() => _context.HasPendingFutureSaves.ShouldBeFalse();
    [Fact] void should_have_no_failures() => _context.FailedPartitions.ShouldBeEmpty();
    [Fact] void should_not_need_initial_state() => _context.NeedsInitialState.ShouldBeFalse();
    [Fact] void should_not_be_unresolvable() => _context.IsUnresolvable.ShouldBeFalse();
}
