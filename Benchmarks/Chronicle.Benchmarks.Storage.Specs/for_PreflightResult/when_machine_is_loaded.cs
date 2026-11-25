// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_PreflightResult;

public class when_machine_is_loaded : Specification
{
    PreflightResult _result = default!;

    void Because() => _result = PreflightResult.Check(StorageRunOptions.Parse(["--profile", "smoke"]), new(13, 23, 25), 12);

    [Fact] void should_not_start() => _result.CanStart.ShouldBeFalse();
    [Fact] void should_use_the_load_exit_code() => _result.ExitCode.ShouldEqual(MachineLoad.RefusedExitCode);
}
