// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compatibility.for_WireCompatibilityChecker.given;

namespace Cratis.Chronicle.Compatibility.for_WireCompatibilityChecker.when_the_contract_narrowed;

public class and_a_field_is_gone : Specification
{
    WireCompatibilityReport _result;

    void Because() => _result = WireCompatibilityChecker.Check(
        WireContracts.With(),
        WireContracts.With(field: WireContracts.DefaultField with { Number = 7 }));

    [Fact] void should_report_incompatible() => _result.IsCompatible.ShouldBeFalse();

    [Fact] void should_report_the_field_number_that_is_gone() =>
        _result.Incompatibilities.ShouldContain(_ =>
            _.Kind == WireIncompatibilityKind.FieldRemoved && _.Description.Contains("Field number 1", StringComparison.Ordinal));
}
