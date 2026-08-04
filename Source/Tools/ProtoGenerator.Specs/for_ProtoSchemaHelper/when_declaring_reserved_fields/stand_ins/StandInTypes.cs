// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace Cratis.Chronicle.Tools.ProtoGenerator.for_ProtoSchemaHelper.when_declaring_reserved_fields.stand_ins;

/// <summary>
/// Stands in for the contracts attribute of the same name. The generator matches by name rather than by type, so a
/// stand-in is what these specifications need to cover the shapes the real contracts have no reason to carry - an
/// attribute with no numbers, a type whose message is missing from the schema.
/// </summary>
/// <remarks>
/// It lives in its own namespace so it cannot shadow the real one where that is what is under test. That the two
/// names still agree is pinned by <c>and_the_real_contract_attribute_is_used</c>, not assumed here.
/// </remarks>
/// <param name="fieldNumbers">The retired field numbers.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class ReservedProtoFieldsAttribute(params int[] fieldNumbers) : Attribute
{
    /// <summary>
    /// Gets the retired field numbers.
    /// </summary>
    public IReadOnlyList<int> FieldNumbers { get; } = fieldNumbers;
}

[ReservedProtoFields(3, 1, 7)]
public class TypeWithRetiredFields;

[ReservedProtoFields]
public class TypeReservingNothing;

public class TypeWithNoAttribute;
