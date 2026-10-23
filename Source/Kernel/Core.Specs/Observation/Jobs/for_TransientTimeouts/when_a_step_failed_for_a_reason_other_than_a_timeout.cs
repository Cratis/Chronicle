// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_TransientTimeouts;

public class when_a_step_failed_for_a_reason_other_than_a_timeout : given.a_step_that_ended_with_a_timeout
{
    IObserver _observer;

    void Establish() => _observer = Substitute.For<IObserver>();

    async Task Because() => await TransientTimeouts.ReportPartitionFailedWhenAttemptsAreExhausted(_observer, _otherFailure, _options);

    [Fact] void should_leave_reporting_to_the_step() =>
        _observer.DidNotReceive().PartitionFailedWithDeliveryFence(Arg.Any<Key>(), Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<FailureKind>(), Arg.Any<PartitionFailureContext>());
}
