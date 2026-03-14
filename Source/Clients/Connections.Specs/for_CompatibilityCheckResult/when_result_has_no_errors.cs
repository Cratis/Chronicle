// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_CompatibilityCheckResult;

public class when_result_has_no_errors : Specification
{
    CompatibilityCheckResult _result;

    void Establish() => _result = new CompatibilityCheckResult([]);

    [Fact] void should_be_compatible() => _result.IsCompatible.ShouldBeTrue();
    [Fact] void should_have_no_errors() => _result.Errors.ShouldBeEmpty();
}
