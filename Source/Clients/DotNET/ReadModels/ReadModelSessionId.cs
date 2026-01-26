// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Represents the unique identifier of a read model session.
/// </summary>
/// <param name="Value">The inner value.</param>
public record ReadModelSessionId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>
    /// Gets the not set value.
    /// </summary>
    public static readonly ReadModelSessionId NotSet = new(Guid.Empty);

    /// <summary>
    /// Implicitly convert from <see cref="Guid"/> to <see cref="ReadModelSessionId"/>.
    /// </summary>
    /// <param name="value">The <see cref="Guid"/> to convert from.</param>
    public static implicit operator ReadModelSessionId(Guid value) => new(value);

    /// <summary>
    /// Create a new unique <see cref="ReadModelSessionId"/>.
    /// </summary>
    /// <returns>A new <see cref="ReadModelSessionId"/>.</returns>
    public static ReadModelSessionId New() => new(Guid.NewGuid());
}
