// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_KernelExit.when_deciding_the_exit_code;

public class and_the_stop_timed_out : Specification
{
    int _code;

    void Because() => _code = KernelExit.CodeFor(KernelRunOutcome.StopTimedOut, 0);

    [Fact] void should_exit_with_the_failed_code() => _code.ShouldEqual(KernelExit.FailedExitCode);
}
