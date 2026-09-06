// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compatibility.for_WireCompatibilityChecker.given;

namespace Cratis.Chronicle.Compatibility.for_WireCompatibilityChecker.when_the_contract_narrowed;

public class and_a_field_changed : Specification
{
    [Fact]
    void should_report_a_changed_type() =>
        KindsFor(WireContracts.DefaultField with { TypeName = "int32" }).ShouldContain(WireIncompatibilityKind.FieldTypeChanged);

    [Fact]
    void should_report_a_change_between_singular_and_repeated() =>
        KindsFor(WireContracts.DefaultField with { Label = WireFieldLabel.Repeated }).ShouldContain(WireIncompatibilityKind.FieldLabelChanged);

    [Fact]
    void should_report_a_rename_even_though_binary_decoding_survives_it() =>
        KindsFor(WireContracts.DefaultField with { Name = "Renamed" }).ShouldContain(WireIncompatibilityKind.FieldRenamed);

    [Fact]
    void should_report_a_move_into_a_oneof() =>
        KindsFor(WireContracts.DefaultField with { OneOf = "either" }).ShouldContain(WireIncompatibilityKind.FieldOneOfChanged);

    static IEnumerable<WireIncompatibilityKind> KindsFor(WireField changed) =>
        WireCompatibilityChecker.Check(WireContracts.With(), WireContracts.With(field: changed)).Incompatibilities.Select(_ => _.Kind);
}
