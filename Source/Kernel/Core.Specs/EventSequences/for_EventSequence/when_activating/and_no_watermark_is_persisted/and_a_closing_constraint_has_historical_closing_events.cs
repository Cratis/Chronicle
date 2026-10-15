// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_activating.and_no_watermark_is_persisted;

public class and_a_closing_constraint_has_historical_closing_events : given.a_catching_up_sequence
{
    protected override bool Closing => true;

    async Task Establish()
    {
        await Seed(0);
        await Seed(1);
        await Seed(2);
    }

    Task Because() => Tick();

    [Fact] void should_start_closing_coverage_at_the_upgrade() => _snapshots.Written!.ConstraintIndexWatermark.ShouldEqual((EventSequenceNumber)3);
    [Fact] void should_not_catch_up_closing_constraints() => _snapshots.Written!.ConstraintIndexCatchUpBelow.ShouldBeNull();
    [Fact] void should_not_write_historical_closures() => _closed.DidNotReceive().Close(Arg.Any<Concepts.Events.Constraints.ClosedStream>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_read_historical_events() => _rangeReads.ShouldEqual(0);
}
