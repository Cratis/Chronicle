// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Cratis.Chronicle.Concepts.Keys;

/// <summary>
/// Identifies the kind of a key value.
/// </summary>
[SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "The key kinds identify the runtime types of key values.")]
public enum KeyKind
{
    /// <summary>
    /// A string, a missing value, or a scalar without its own kind, rendered through its string form.
    /// </summary>
    String = 0,

    /// <summary>
    /// A globally unique identifier.
    /// </summary>
    Guid = 1,

    /// <summary>
    /// A 32-bit signed integer.
    /// </summary>
    Int = 2,

    /// <summary>
    /// A 64-bit signed integer.
    /// </summary>
    Long = 3,

    /// <summary>
    /// A key made of named parts, represented by an ExpandoObject or IDictionary&lt;string, object?&gt;.
    /// </summary>
    Composite = 4
}
