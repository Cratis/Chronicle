// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compatibility.for_WireCompatibilityChecker.given;

namespace Cratis.Chronicle.Compatibility.for_WireCompatibilityChecker.when_the_contract_narrowed;

public class and_a_method_is_gone : Specification
{
    WireCompatibilityReport _result;

    void Because() => _result = WireCompatibilityChecker.Check(
        WireContracts.With(),
        WireContracts.With(method: WireContracts.DefaultMethod with { Name = "DoSomethingElse" }));

    [Fact] void should_report_incompatible() => _result.IsCompatible.ShouldBeFalse();

    [Fact] void should_report_the_method_that_is_gone() =>
        _result.Incompatibilities.ShouldContain(_ =>
            _.Kind == WireIncompatibilityKind.MethodRemoved && _.Path == $"{WireContracts.Service}/Do");

    [Fact] void should_not_report_the_method_that_was_added() =>
        _result.Incompatibilities.ShouldNotContain(_ => _.Path.EndsWith("DoSomethingElse", StringComparison.Ordinal));
}
