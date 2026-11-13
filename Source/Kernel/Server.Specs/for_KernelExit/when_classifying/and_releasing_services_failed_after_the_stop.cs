// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_KernelExit.when_classifying;

public class and_releasing_services_failed_after_the_stop : Specification
{
    KernelRunOutcome _outcome;

    void Because() => _outcome = KernelExit.Classify(KernelRunPhase.Disposing, new ObjectDisposedException("keeper"), stopRequested: true, stopTimedOut: false);

    [Fact] void should_be_a_failed_release() => _outcome.ShouldEqual(KernelRunOutcome.DisposeFailed);
}
