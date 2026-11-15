// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_KernelExit.when_classifying;

public class and_a_stop_was_requested_during_startup : Specification
{
    KernelRunOutcome _outcome;

    void Because() => _outcome = KernelExit.Classify(KernelRunPhase.Starting, new OperationCanceledException(), stopRequested: true, stopTimedOut: false);

    [Fact] void should_be_a_stop_during_startup() => _outcome.ShouldEqual(KernelRunOutcome.StoppedDuringStartup);
}
