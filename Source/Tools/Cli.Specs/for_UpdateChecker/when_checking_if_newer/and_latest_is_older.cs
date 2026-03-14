// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Cli.for_UpdateChecker.when_checking_if_newer;

public class and_latest_is_older : Specification
{
    bool _result;

    void Because() => _result = UpdateChecker.IsNewer("1.0.0", "2.0.0");

    [Fact] void should_not_be_newer() => _result.ShouldBeFalse();
}
