// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_KernelExit.when_classifying;

public class and_the_run_ended : Specification
{
    KernelRunOutcome _outcome;

    void Because() => _outcome = KernelExit.Classify(KernelRunPhase.Ended, null, stopRequested: true, stopTimedOut: false);

    [Fact] void should_be_completed() => _outcome.ShouldEqual(KernelRunOutcome.Completed);
}
