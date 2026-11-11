// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_KernelExit.when_classifying;

public class and_startup_failed_after_a_stop_was_requested : Specification
{
    KernelRunOutcome _outcome;

    void Because() => _outcome = KernelExit.Classify(KernelRunPhase.Starting, new InvalidOperationException(), stopRequested: true, stopTimedOut: false);

    [Fact] void should_be_a_failed_start() => _outcome.ShouldEqual(KernelRunOutcome.StartFailed);
}
