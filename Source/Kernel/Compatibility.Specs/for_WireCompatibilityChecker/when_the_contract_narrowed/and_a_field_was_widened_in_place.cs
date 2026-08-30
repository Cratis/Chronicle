// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compatibility.for_WireCompatibilityChecker.given;

namespace Cratis.Chronicle.Compatibility.for_WireCompatibilityChecker.when_the_contract_narrowed;

/// <summary>
/// Widening a string field to repeated is what Chronicle did to <c>Constraint.RemovedWith</c>, deliberately and in
/// the belief that it was non-breaking. It half is: one occurrence of a length-delimited field encodes identically
/// either way, so binary decoding survives - but every generated client changes the property's type, which is how
/// the TypeScript client stopped compiling. Saying which of the two it is decides what the fix has to be.
/// </summary>
public class and_a_field_was_widened_in_place : Specification
{
    [Fact]
    void should_say_binary_decoding_survives_a_widened_string() =>
        DescriptionFor(WireContracts.DefaultField with { Label = WireFieldLabel.Repeated })
            .ShouldContain("Binary decoding survives it");

    [Fact]
    void should_not_say_that_of_a_widened_number() =>
        DescriptionFor(WireContracts.DefaultField with { TypeName = "int32", Label = WireFieldLabel.Repeated },
                       WireContracts.DefaultField with { TypeName = "int32" })
            .ShouldNotContain("Binary decoding survives it");

    [Fact]
    void should_not_say_that_of_a_narrowing_back_to_singular() =>
        DescriptionFor(WireContracts.DefaultField,
                       WireContracts.DefaultField with { Label = WireFieldLabel.Repeated })
            .ShouldNotContain("Binary decoding survives it");

    static string DescriptionFor(WireField changed, WireField? original = null) =>
        WireCompatibilityChecker
            .Check(WireContracts.With(field: original ?? WireContracts.DefaultField), WireContracts.With(field: changed))
            .Incompatibilities
            .Single(_ => _.Kind == WireIncompatibilityKind.FieldLabelChanged)
            .Description;
}
