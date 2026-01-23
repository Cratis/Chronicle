// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Api.Users;

/// <summary>
/// Represents the unique identifier for a user.
/// </summary>
/// <param name="Value">The inner value.</param>
public record UserId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>
    /// Gets the Admin user identifier.
    /// </summary>
    public static readonly UserId Admin = new(Guid.Parse("00000000-0000-0000-0000-000000000001"));

    /// <summary>
    /// Gets the not set <see cref="UserId"/>.
    /// </summary>
    public static readonly UserId NotSet = new(Guid.Empty);

    /// <summary>
    /// Implicitly convert from <see cref="Guid"/> to <see cref="UserId"/>.
    /// </summary>
    /// <param name="value"><see cref="Guid"/> representation.</param>
    public static implicit operator UserId(Guid value) => new(value);

    /// <summary>
    /// Implicitly convert from <see cref="UserId"/> to <see cref="Guid"/>.
    /// </summary>
    /// <param name="userId"><see cref="UserId"/> to convert from.</param>
    public static implicit operator Guid(UserId userId) => userId.Value;

    /// <summary>
    /// Implicitly convert from <see cref="UserId"/> to <see cref="EventSourceId"/>.
    /// </summary>
    /// <param name="userId"><see cref="UserId"/> to convert from.</param>
    public static implicit operator EventSourceId(UserId userId) => new(userId.Value.ToString());

    /// <summary>
    /// Create a new <see cref="UserId"/>.
    /// </summary>
    /// <returns>A new <see cref="UserId"/>.</returns>
    public static UserId New() => new(Guid.NewGuid());

    /// <summary>
    /// Parse a string to a <see cref="UserId"/>.
    /// </summary>
    /// <param name="value">The string value to parse.</param>
    /// <returns>The parsed <see cref="UserId"/>.</returns>
    public static UserId Parse(string value) => new(Guid.Parse(value));
}
