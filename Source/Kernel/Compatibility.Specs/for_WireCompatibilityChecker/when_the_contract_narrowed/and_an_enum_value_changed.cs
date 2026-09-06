// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compatibility.for_WireCompatibilityChecker.given;

namespace Cratis.Chronicle.Compatibility.for_WireCompatibilityChecker.when_the_contract_narrowed;

public class and_an_enum_value_changed : Specification
{
    [Fact]
    void should_report_a_value_that_is_gone() =>
        KindsFor((3, "Red")).ShouldContain(WireIncompatibilityKind.EnumValueRemoved);

    [Fact]
    void should_report_a_value_that_was_renamed() =>
        KindsFor((0, "Crimson")).ShouldContain(WireIncompatibilityKind.EnumValueRenamed);

    static IEnumerable<WireIncompatibilityKind> KindsFor((int Number, string Name) changed) =>
        WireCompatibilityChecker.Check(WireContracts.With(), WireContracts.With(enumValue: changed)).Incompatibilities.Select(_ => _.Kind);
}
