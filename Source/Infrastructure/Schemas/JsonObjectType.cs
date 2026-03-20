// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Represents the JSON object type flags.
/// </summary>
[Flags]
public enum JsonObjectType
{
    /// <summary>No type.</summary>
    None = 0,

    /// <summary>Array type.</summary>
    Array = 1,

    /// <summary>Boolean type.</summary>
    Boolean = 2,

    /// <summary>Integer type.</summary>
    Integer = 4,

    /// <summary>Null type.</summary>
    Null = 8,

    /// <summary>Number type.</summary>
    Number = 16,

    /// <summary>Object type.</summary>
    Object = 32,

    /// <summary>String type.</summary>
    String = 64
}
