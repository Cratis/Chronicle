// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Auditing;

/// <summary>
/// Represents a causation instance.
/// </summary>
/// <param name="Occurred">When it occurred.</param>
/// <param name="Type">Type of causation.</param>
/// <param name="Properties">Any properties associated with the causation.</param>
public record Causation(
    DateTimeOffset Occurred,
    CausationType Type,
    IDictionary<string, string> Properties)
{
    /// <summary>
    /// Creates an unknown causation instance.
    /// </summary>
    /// <returns>A new instance of <see cref="Causation"/> with the current time, type set to <see cref="CausationType.Unknown"/>, and an empty properties dictionary.</returns>
    public static Causation Unknown() => new(
        DateTimeOffset.UtcNow,
        CausationType.Unknown,
        new Dictionary<string, string>());
}
