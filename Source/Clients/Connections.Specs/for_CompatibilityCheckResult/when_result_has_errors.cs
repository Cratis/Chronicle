// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_CompatibilityCheckResult;

public class when_result_has_errors : Specification
{
    CompatibilityCheckResult _result;

    void Establish() => _result = new CompatibilityCheckResult(["Error 1", "Error 2"]);

    [Fact] void should_not_be_compatible() => _result.IsCompatible.ShouldBeFalse();
    [Fact] void should_have_errors() => _result.Errors.ShouldNotBeEmpty();
    [Fact] void should_have_correct_error_count() => _result.Errors.Count().ShouldEqual(2);
}
