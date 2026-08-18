// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compatibility;

/// <summary>
/// Represents the kinds of incompatibility that can exist between two versions of the wire contract.
/// </summary>
public enum WireIncompatibilityKind
{
    /// <summary>A service the older side calls no longer exists.</summary>
    ServiceRemoved = 0,

    /// <summary>A method the older side calls no longer exists on its service.</summary>
    MethodRemoved = 1,

    /// <summary>A method still exists but takes or returns a different message.</summary>
    MethodSignatureChanged = 2,

    /// <summary>A method changed between unary and streaming on either side of the call.</summary>
    MethodStreamingChanged = 3,

    /// <summary>A message the older side sends or receives no longer exists.</summary>
    MessageRemoved = 4,

    /// <summary>A field number the older side writes is no longer read.</summary>
    FieldRemoved = 5,

    /// <summary>A field number is still read, but as a different type.</summary>
    FieldTypeChanged = 6,

    /// <summary>A field number changed between singular, repeated and map.</summary>
    FieldLabelChanged = 7,

    /// <summary>A field number kept its type but was renamed, which breaks generated code and JSON.</summary>
    FieldRenamed = 8,

    /// <summary>A field number moved into or out of a oneof, changing which fields clear each other.</summary>
    FieldOneOfChanged = 9,

    /// <summary>An enum the older side sends no longer exists.</summary>
    EnumRemoved = 10,

    /// <summary>An enum value the older side sends is no longer defined.</summary>
    EnumValueRemoved = 11,

    /// <summary>An enum value kept its number but was renamed.</summary>
    EnumValueRenamed = 12
}
