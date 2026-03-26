// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Represents the JSON object type flags.
/// </summary>
[Flags]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "JSON Schema type names must match the specification")]
public enum JsonObjectType
{
    /// <summary>No type.</summary>
    None = 0,

    /// <summary>Array type.</summary>
    Array = 1 << 0,

    /// <summary>Boolean type.</summary>
    Boolean = 1 << 1,

    /// <summary>Integer type.</summary>
    Integer = 1 << 2,

    /// <summary>Null type.</summary>
    Null = 1 << 3,

    /// <summary>Number type.</summary>
    Number = 1 << 4,

    /// <summary>Object type.</summary>
    Object = 1 << 5,

    /// <summary>String type.</summary>
    String = 1 << 6,
}
