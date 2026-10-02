// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Alerts;

/// <summary>
/// Represents the partition an alert is about, or <see cref="None"/> when the alert is about the whole observer.
/// </summary>
/// <remarks>
/// Alert events must not carry nullable properties, so the absence of a partition is a named sentinel rather than
/// <c language="csharp">null</c>. Which conditions are about a partition is known from the condition itself
/// (<see cref="AlertConditionKind.PartitionFailing"/> and <see cref="AlertConditionKind.PartitionRetriesExhausted"/>
/// are, <see cref="AlertConditionKind.ObserverQuarantined"/> is not), so a partition that happens to be named like the
/// sentinel cannot be mistaken for an observer-wide alert.
/// </remarks>
/// <param name="Value">The partition key rendered as a string.</param>
public record AlertPartition(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the <see cref="AlertPartition"/> used when an alert is not about a single partition.
    /// </summary>
    public static readonly AlertPartition None = new("[none]");

    /// <summary>
    /// Implicitly convert from a string to <see cref="AlertPartition"/>.
    /// </summary>
    /// <param name="value">String to convert from.</param>
    public static implicit operator AlertPartition(string value) => new(value);
}
