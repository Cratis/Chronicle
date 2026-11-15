// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_KernelExit.when_deciding_the_exit_code;

public class and_a_stop_was_requested_during_startup : Specification
{
    int _exitCode;

    void Because() => _exitCode = KernelExit.CodeFor(KernelRunOutcome.StoppedDuringStartup, 0);

    [Fact] void should_exit_with_zero() => _exitCode.ShouldEqual(0);
}
