// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Defines a system that can update the index of a constraint.
/// </summary>
public interface IUpdateConstraintIndex
{
    /// <summary>
    /// Update any affected constraints with information from the <see cref="EventToValidateForConstraints"/> and <see cref="EventSequenceNumber"/>.
    /// </summary>
    /// <param name="eventSequenceNumber">The <see cref="EventSequenceNumber"/> of the event.</param>
    /// <returns>Awaitable task.</returns>
    public Task Update(EventSequenceNumber eventSequenceNumber);
}
