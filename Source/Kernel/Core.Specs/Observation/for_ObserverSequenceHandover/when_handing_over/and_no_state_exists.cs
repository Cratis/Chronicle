// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverSequenceHandover.when_handing_over;

public class and_no_state_exists : given.a_handover
{
    void Establish() => _current = new() { Identifier = ObserverId.Unspecified };

    async Task Because() => _result = await HandOver();

    [Fact] void should_report_that_ownership_was_claimed() => _result.ShouldEqual(SequenceHandoverResult.Claimed);
    [Fact] void should_claim_ownership_for_the_target() => _observers.Received(1).ClaimOwnership(_observerId, _to);
    [Fact] void should_not_select_anything() => _observers.DidNotReceive().HandOver(Arg.Any<ObserverId>(), Arg.Any<EventSequenceId?>(), Arg.Any<ObserverSequenceCheckpoint>());
    [Fact] void should_not_touch_the_checkpoints() => _checkpoints.ReceivedCalls().ShouldBeEmpty();
}
