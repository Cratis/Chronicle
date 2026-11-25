// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_PreflightResult;

public class when_medium_profile_is_not_confirmed : Specification
{
    PreflightResult _result = default!;
    PreflightResult _confirmed = default!;

    void Because()
    {
        _result = PreflightResult.Check(StorageRunOptions.Parse(["--profile", "medium"]), new(0.1, 0.1, 0.1), 12);
        _confirmed = PreflightResult.Check(StorageRunOptions.Parse(["--profile", "medium", StorageRunOptions.ConfirmFlag]), new(0.1, 0.1, 0.1), 12);
    }

    [Fact] void should_ask_for_confirmation() => _result.ExitCode.ShouldEqual(2);
    [Fact] void should_start_once_confirmed() => _confirmed.CanStart.ShouldBeTrue();
}
