// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverSequenceHandover.when_handing_over;

public class and_the_owner_changes_once : given.a_handover
{
    void Establish() => _observers.HandOver(_observerId, Arg.Any<EventSequenceId?>(), Arg.Any<ObserverSequenceCheckpoint>()).Returns(false, true);

    async Task Because() => _result = await HandOver();

    [Fact] void should_report_the_handover() => _result.ShouldEqual(SequenceHandoverResult.HandedOver);
    [Fact] void should_retry_once() => _observers.Received(2).HandOver(_observerId, Arg.Any<EventSequenceId?>(), Arg.Any<ObserverSequenceCheckpoint>());
}
