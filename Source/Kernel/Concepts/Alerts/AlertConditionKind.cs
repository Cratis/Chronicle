// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Alerts;

/// <summary>
/// Represents the kind of condition an alert is raised for.
/// </summary>
/// <remarks>
/// The value is the stable, hyphenated name that is both stored in the alert events and used as the key under
/// <c language="csharp">alerts.conditions</c> in the configuration. It is a string rather than an enum so that the configuration keys
/// bind without a mapping and later slices can add kinds, such as lag and stall, without renumbering anything.
/// </remarks>
/// <param name="Value">The hyphenated name of the condition.</param>
public record AlertConditionKind(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// A partition has been failing for longer than its grace period while still being retried.
    /// </summary>
    public static readonly AlertConditionKind PartitionFailing = new("partition-failing");

    /// <summary>
    /// A partition has run out of retries and a person has to act.
    /// </summary>
    public static readonly AlertConditionKind PartitionRetriesExhausted = new("partition-retries-exhausted");

    /// <summary>
    /// An observer has been quarantined.
    /// </summary>
    public static readonly AlertConditionKind ObserverQuarantined = new("observer-quarantined");

    /// <summary>
    /// Implicitly convert from a string to <see cref="AlertConditionKind"/>.
    /// </summary>
    /// <param name="value">String to convert from.</param>
    public static implicit operator AlertConditionKind(string value) => new(value);
}
