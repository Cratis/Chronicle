// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Stable version-1 content tags. Values must never be reassigned or reused.
/// </summary>
internal enum CompactContentTag : byte
{
    /// <summary>A null value.</summary>
    Null = 0,

    /// <summary>An ordered object.</summary>
    Object = 1,

    /// <summary>An ordered array.</summary>
    Array = 2,

    /// <summary>A UTF-8 string.</summary>
    String = 3,

    /// <summary>A boolean.</summary>
    Boolean = 4,

    /// <summary>An unsigned byte.</summary>
    Byte = 5,

    /// <summary>A signed byte.</summary>
    SByte = 6,

    /// <summary>A signed 16-bit integer.</summary>
    Int16 = 7,

    /// <summary>An unsigned 16-bit integer.</summary>
    UInt16 = 8,

    /// <summary>A signed 32-bit integer.</summary>
    Int32 = 9,

    /// <summary>An unsigned 32-bit integer.</summary>
    UInt32 = 10,

    /// <summary>A signed 64-bit integer.</summary>
    Int64 = 11,

    /// <summary>An unsigned 64-bit integer.</summary>
    UInt64 = 12,

    /// <summary>An IEEE single-precision value.</summary>
    Single = 13,

    /// <summary>An IEEE double-precision value.</summary>
    Double = 14,

    /// <summary>A decimal including its scale.</summary>
    Decimal = 15,

    /// <summary>A GUID.</summary>
    Guid = 16,

    /// <summary>A date-time including its kind.</summary>
    DateTime = 17,

    /// <summary>A date-time including its offset.</summary>
    DateTimeOffset = 18,

    /// <summary>A date.</summary>
    DateOnly = 19,

    /// <summary>A time of day.</summary>
    TimeOnly = 20,

    /// <summary>A signed duration.</summary>
    TimeSpan = 21,

    /// <summary>A byte array.</summary>
    Bytes = 22
}
