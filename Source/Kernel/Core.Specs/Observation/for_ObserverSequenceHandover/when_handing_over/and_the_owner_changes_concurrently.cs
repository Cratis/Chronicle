// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverSequenceHandover.when_handing_over;

public class and_the_owner_changes_concurrently : given.a_handover
{
    Exception? _error;

    void Establish() => _observers.HandOver(_observerId, Arg.Any<EventSequenceId?>(), Arg.Any<ObserverSequenceCheckpoint>()).Returns(false);

    async Task Because() => _error = await Catch.Exception(async () => _result = await HandOver());

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_report_that_nothing_was_handed_over() => _result.ShouldEqual(SequenceHandoverResult.NotHandedOver);
    [Fact] void should_re_read_the_state_for_every_attempt() => _observers.Received(3).Get(_observerId);
    [Fact] void should_stop_after_three_attempts() => _observers.Received(3).HandOver(_observerId, Arg.Any<EventSequenceId?>(), Arg.Any<ObserverSequenceCheckpoint>());
}
