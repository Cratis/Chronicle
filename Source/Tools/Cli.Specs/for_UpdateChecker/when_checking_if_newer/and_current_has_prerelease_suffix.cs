// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Cli.for_UpdateChecker.when_checking_if_newer;

public class and_current_has_prerelease_suffix : Specification
{
    bool _result;

    void Because() => _result = UpdateChecker.IsNewer("2.0.0", "1.5.0-preview.1");

    [Fact] void should_be_newer() => _result.ShouldBeTrue();
}
