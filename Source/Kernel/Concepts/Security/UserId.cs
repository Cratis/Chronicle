// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.Security;

/// <summary>
/// Represents a user identifier.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record UserId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>
    /// Represents an unset <see cref="UserId"/>.
    /// </summary>
    public static readonly UserId NotSet = new(Guid.Empty);

    /// <summary>
    /// Implicitly converts from <see cref="EventSourceId"/> to <see cref="UserId"/>.
    /// </summary>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to convert.</param>
    /// <returns>The converted <see cref="UserId"/>.</returns>
    public static implicit operator UserId(EventSourceId eventSourceId) => new(Guid.Parse(eventSourceId.Value));

    /// <summary>
    /// Implicitly converts from <see cref="Guid"/> to <see cref="UserId"/>.
    /// </summary>
    /// <param name="value">The <see cref="Guid"/> to convert.</param>
    /// <returns>The converted <see cref="UserId"/>.</returns>
    public static implicit operator UserId(Guid value) => new(value);
}
