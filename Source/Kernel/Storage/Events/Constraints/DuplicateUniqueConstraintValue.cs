// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.Events.Constraints;

/// <summary>
/// The exception that is thrown when a unique constraint value is claimed by an event source while another event
/// source already holds it.
/// </summary>
/// <param name="constraintName">The <see cref="ConstraintName"/> that was violated.</param>
/// <param name="eventSourceId">The <see cref="EventSourceId"/> that tried to claim the value.</param>
/// <remarks>
/// Validation normally rejects a duplicate before the event is appended, so this represents a claim that raced past
/// validation and was only caught by the store enforcing uniqueness. It is a constraint violation rather than a
/// storage malfunction, and every <see cref="IUniqueConstraintsStorage"/> implementation reports it this way so
/// callers never have to recognize a provider-specific error.
/// </remarks>
public class DuplicateUniqueConstraintValue(ConstraintName constraintName, EventSourceId eventSourceId)
    : Exception($"Event source '{eventSourceId}' cannot claim the value it holds for unique constraint '{constraintName}' - another event source already holds it.")
{
    /// <summary>
    /// Gets the <see cref="ConstraintName"/> that was violated.
    /// </summary>
    public ConstraintName ConstraintName { get; } = constraintName;

    /// <summary>
    /// Gets the <see cref="EventSourceId"/> that tried to claim the value.
    /// </summary>
    public EventSourceId EventSourceId { get; } = eventSourceId;
}
