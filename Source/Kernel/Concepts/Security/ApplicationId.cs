// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.Security;

/// <summary>
/// Represents an application identifier.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record ApplicationId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>
    /// Represents an unset <see cref="ApplicationId"/>.
    /// </summary>
    public static readonly ApplicationId NotSet = new(Guid.Empty);

    /// <summary>
    /// Implicitly converts from <see cref="Guid"/> to <see cref="ApplicationId"/>.
    /// </summary>
    /// <param name="value">The <see cref="Guid"/> to convert.</param>
    /// <returns>The converted <see cref="ApplicationId"/>.</returns>
    public static implicit operator ApplicationId(Guid value) => new(value);

    /// <summary>
    /// Implicitly converts from <see cref="EventSourceId"/> to <see cref="ApplicationId"/>.
    /// </summary>
    /// <param name="value">The <see cref="EventSourceId"/> to convert.</param>
    /// <returns>The converted <see cref="ApplicationId"/>.</returns>
    public static implicit operator ApplicationId(EventSourceId value) => new(Guid.Parse(value.Value));

    /// <summary>
    /// Implicitly converts from <see cref="string"/> to <see cref="ApplicationId"/>.
    /// </summary>
    /// <param name="value">The <see cref="string"/> to convert.</param>
    /// <returns>The converted <see cref="ApplicationId"/>.</returns>
    public static implicit operator ApplicationId(string value) => new(Guid.Parse(value));
}
