// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_KernelExit.when_deciding_the_exit_code;

public class and_the_stop_failed_after_the_kernel_requested_a_code : Specification
{
    int _exitCode;

    void Because() => _exitCode = KernelExit.CodeFor(KernelRunOutcome.StopFailed, 1);

    [Fact] void should_keep_the_requested_code() => _exitCode.ShouldEqual(1);
}
