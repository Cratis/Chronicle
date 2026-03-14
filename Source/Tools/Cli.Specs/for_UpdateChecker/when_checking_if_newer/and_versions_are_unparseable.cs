// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Cli.for_UpdateChecker.when_checking_if_newer;

public class and_versions_are_unparseable : Specification
{
    bool _result;

    void Because() => _result = UpdateChecker.IsNewer("not-a-version", "also-not");

    [Fact] void should_not_be_newer() => _result.ShouldBeFalse();
}
