// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compatibility.for_WireCompatibilityChecker.given;

namespace Cratis.Chronicle.Compatibility.for_WireCompatibilityChecker.when_the_contract_narrowed;

public class and_a_method_takes_something_else : Specification
{
    WireCompatibilityReport _result;

    void Because() => _result = WireCompatibilityChecker.Check(
        WireContracts.With(),
        WireContracts.With(method: WireContracts.DefaultMethod with { InputType = ".test.SomethingElse" }));

    [Fact] void should_report_incompatible() => _result.IsCompatible.ShouldBeFalse();

    [Fact] void should_report_the_changed_signature() =>
        _result.Incompatibilities.ShouldContain(_ => _.Kind == WireIncompatibilityKind.MethodSignatureChanged);

    [Fact] void should_say_what_it_changed_from_and_to() =>
        _result.Incompatibilities.Single(_ => _.Kind == WireIncompatibilityKind.MethodSignatureChanged)
            .Description.ShouldContain(".test.SomethingElse");
}
