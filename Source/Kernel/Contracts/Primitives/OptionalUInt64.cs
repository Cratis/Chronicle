// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Primitives;

/// <summary>
/// Represents a present unsigned 64-bit value, including zero. An absent wrapper means no value.
/// </summary>
[ProtoContract]
public class OptionalUInt64
{
    /// <summary>
    /// Gets or sets the wrapped value.
    /// </summary>
    [ProtoMember(1)]
    public ulong Value { get; set; }

    /// <summary>
    /// Converts a nullable value into a wrapper only when the value is present.
    /// </summary>
    /// <param name="value">The value to wrap.</param>
    /// <returns>The wrapper, or null if the value is absent.</returns>
    public static implicit operator OptionalUInt64?(ulong? value) =>
        value is { } present ? new() { Value = present } : null;

    /// <summary>
    /// Converts the wrapper back to a nullable value.
    /// </summary>
    /// <param name="wrapper">The wrapper to read.</param>
    /// <returns>The wrapped value, or null if the wrapper is absent.</returns>
    public static implicit operator ulong?(OptionalUInt64? wrapper) => wrapper?.Value;
}
