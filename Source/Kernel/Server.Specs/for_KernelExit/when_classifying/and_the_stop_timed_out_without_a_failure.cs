// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_KernelExit.when_classifying;

public class and_the_stop_timed_out_without_a_failure : Specification
{
    KernelRunOutcome _outcome;

    void Because() => _outcome = KernelExit.Classify(KernelRunPhase.Ended, null, stopRequested: true, stopTimedOut: true);

    [Fact] void should_be_a_timed_out_stop() => _outcome.ShouldEqual(KernelRunOutcome.StopTimedOut);
}
