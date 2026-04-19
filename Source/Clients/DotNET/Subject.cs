// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle;

/// <summary>
/// Represents the subject an event or read model is about — the target used as the identity for
/// compliance concerns such as PII encryption keys. When omitted on append, the
/// <see cref="EventSourceId"/> is used as the subject.
/// </summary>
/// <param name="Value">Actual value.</param>
public record Subject(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the representation of a <see cref="Subject"/> that has not been set.
    /// </summary>
    public static readonly Subject NotSet = new(string.Empty);

    /// <summary>
    /// Gets a value indicating whether the <see cref="Subject"/> has been set.
    /// </summary>
    public bool IsSet => this != NotSet;

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="Subject"/>.
    /// </summary>
    /// <param name="value"><see cref="string"/> to convert from.</param>
    /// <returns>A converted <see cref="Subject"/>.</returns>
    public static implicit operator Subject(string value) => new(value);

    /// <summary>
    /// Implicitly convert from <see cref="Guid"/> to <see cref="Subject"/>.
    /// </summary>
    /// <param name="id"><see cref="Guid"/> to convert from.</param>
    /// <returns>A converted <see cref="Subject"/>.</returns>
    public static implicit operator Subject(Guid id) => new(id.ToString());

    /// <summary>
    /// Implicitly convert from <see cref="EventSourceId"/> to <see cref="Subject"/>.
    /// </summary>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> to convert from.</param>
    /// <returns>A converted <see cref="Subject"/>.</returns>
    public static implicit operator Subject(EventSourceId eventSourceId) => new(eventSourceId.Value);
}
