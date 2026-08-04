// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.ProtoGenerator.for_ProtoSchemaHelper.when_declaring_reserved_fields;

/// <summary>
/// The generator does not reference the contracts assembly - it loads it - so it finds the attribute by name and
/// reads its numbers by property name. Nothing in the compiler connects those two strings to the real attribute.
/// </summary>
/// <remarks>
/// This is the spec that connects them. Rename the attribute or its property and the generator would simply stop
/// reserving anything, with a green build and a generated schema that silently frees every retired field number
/// for reuse. The other specifications here use local stand-ins, so only this one would notice.
/// </remarks>
public class and_the_real_contract_attribute_is_used : Specification
{
    [Contracts.ReservedProtoFields(4, 9)]
    public class ContractCarryingTheRealAttribute;

    string _result;

    void Because() => _result = ProtoSchemaHelper.DeclareReservedFields(
        "message ContractCarryingTheRealAttribute {\n}",
        [typeof(ContractCarryingTheRealAttribute)]);

    [Fact] void should_find_the_attribute_by_the_name_the_generator_looks_for() => _result.ShouldContain("reserved 4, 9;");

    [Fact]
    void should_read_the_numbers_from_the_property_the_generator_looks_for() =>
        typeof(Contracts.ReservedProtoFieldsAttribute).GetProperty(ProtoSchemaHelper.FieldNumbersPropertyName).ShouldNotBeNull();

    [Fact]
    void should_name_the_attribute_the_generator_looks_for() =>
        nameof(Contracts.ReservedProtoFieldsAttribute).ShouldEqual(ProtoSchemaHelper.ReservedProtoFieldsAttributeName);
}
