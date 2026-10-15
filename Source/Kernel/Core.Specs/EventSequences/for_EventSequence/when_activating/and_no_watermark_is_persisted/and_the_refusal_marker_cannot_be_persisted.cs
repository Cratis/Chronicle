// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_activating.and_no_watermark_is_persisted;

public class and_the_refusal_marker_cannot_be_persisted : given.a_catching_up_sequence
{
    async Task Establish()
    {
        await TransferHistory();
        _snapshots.BeforeWrite = () => Task.FromException(new given.SimulatedStateWriteError());
    }

    Task Because() => Tick();

    [Fact] void should_not_release_the_current_holder() => _unique.DidNotReceive().Remove(Arg.Any<EventSourceId>(), Arg.Any<UniqueConstraintDefinition>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_claim_any_value() => _claimWrites.ShouldEqual(0);
    [Fact] async Task should_keep_the_existing_protection() => (await Values("old-holder")).ShouldContainOnly(Hash("value"));
    [Fact] void should_keep_the_catch_up_pending() => _stateStorage.State.ConstraintIndexCatchUpBelow.ShouldEqual((EventSequenceNumber)3);
}
