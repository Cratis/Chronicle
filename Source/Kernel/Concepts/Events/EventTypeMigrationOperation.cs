// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events;

/// <summary>
/// Represents an event type migration operation identifier.
/// </summary>
/// <param name="Value">The operation identifier.</param>
public record EventTypeMigrationOperation(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the split operation.
    /// </summary>
    public static readonly EventTypeMigrationOperation Split = new("split");

    /// <summary>
    /// Gets the combine operation.
    /// </summary>
    public static readonly EventTypeMigrationOperation Combine = new("combine");

    /// <summary>
    /// Gets the rename operation.
    /// </summary>
    public static readonly EventTypeMigrationOperation Rename = new("rename");

    /// <summary>
    /// Gets the default value operation.
    /// </summary>
    public static readonly EventTypeMigrationOperation DefaultValue = new("defaultValue");

    /// <summary>
    /// Implicitly convert from string to <see cref="EventTypeMigrationOperation"/>.
    /// </summary>
    /// <param name="value">String to convert from.</param>
    public static implicit operator EventTypeMigrationOperation(string value) => new(value);
}
