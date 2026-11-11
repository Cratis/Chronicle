// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_KernelExit.when_classifying;

public class and_a_service_failed_while_stopping : Specification
{
    KernelRunOutcome _outcome;

    void Because() => _outcome = KernelExit.Classify(KernelRunPhase.Stopping, new InvalidOperationException(), stopRequested: true, stopTimedOut: false);

    [Fact] void should_be_a_failed_stop() => _outcome.ShouldEqual(KernelRunOutcome.StopFailed);
}
