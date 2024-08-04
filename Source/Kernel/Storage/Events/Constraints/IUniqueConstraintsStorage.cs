// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.Events.Constraints;

/// <summary>
/// Defines the storage mechanism for unique constraints.
/// </summary>
public interface IUniqueConstraintsStorage
{
    /// <summary>
    /// Check if a constraint value exists.
    /// </summary>
    /// <param name="name"><see cref="ConstraintName"/> to check for.</param>
    /// <param name="value"><see cref="UniqueConstraintValue"/>to check.</param>
    /// <param name="sequenceNumber">The <see cref="EventSequenceNumber"/> of the existing event, if any.</param>
    /// <returns>True if it exists, false if not.</returns>
    Task<bool> Exists(ConstraintName name, UniqueConstraintValue value, out EventSequenceNumber sequenceNumber);

    /// <summary>
    /// Save a constraint value.
    /// </summary>
    /// <param name="name"><see cref="ConstraintName"/> to save for.</param>
    /// <param name="value"><see cref="UniqueConstraintValue"/>to save.</param>
    /// <returns>True if it exists, false if not.</returns>
    Task Save(ConstraintName name, UniqueConstraintValue value);
}
