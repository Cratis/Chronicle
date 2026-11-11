// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_KernelExit.when_deciding_the_exit_code;

public class and_the_stop_did_not_complete : Specification
{
    int _timedOut;
    int _failed;
    int _disposeFailed;

    void Because()
    {
        _timedOut = KernelExit.CodeFor(KernelRunOutcome.StopTimedOut, 0);
        _failed = KernelExit.CodeFor(KernelRunOutcome.StopFailed, 0);
        _disposeFailed = KernelExit.CodeFor(KernelRunOutcome.DisposeFailed, 0);
    }

    [Fact] void should_exit_with_the_failed_code_when_the_stop_timed_out() => _timedOut.ShouldEqual(KernelExit.FailedExitCode);
    [Fact] void should_exit_with_the_failed_code_when_a_service_failed_to_stop() => _failed.ShouldEqual(KernelExit.FailedExitCode);
    [Fact] void should_exit_with_the_failed_code_when_releasing_services_failed() => _disposeFailed.ShouldEqual(KernelExit.FailedExitCode);
    [Fact] void should_use_a_non_zero_failed_code() => KernelExit.FailedExitCode.ShouldNotEqual(0);
}
