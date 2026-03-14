// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Cli.for_UpdateChecker.when_checking_if_newer;

public class and_latest_is_greater_by_patch : Specification
{
    bool _result;

    void Because() => _result = UpdateChecker.IsNewer("1.0.1", "1.0.0");

    [Fact] void should_be_newer() => _result.ShouldBeTrue();
}
