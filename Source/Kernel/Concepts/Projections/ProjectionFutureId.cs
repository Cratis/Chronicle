// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections;

/// <summary>
/// Represents the unique identifier of a <see cref="ProjectionFuture"/>.
/// </summary>
/// <param name="Value">The inner value.</param>
public record ProjectionFutureId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>
    /// Represents a future identifier that is not set.
    /// </summary>
    public static readonly ProjectionFutureId NotSet = new(Guid.Empty);

    /// <summary>
    /// Implicitly convert from a <see cref="Guid"/> to a <see cref="ProjectionFutureId"/>.
    /// </summary>
    /// <param name="value">The <see cref="Guid"/> value.</param>
    public static implicit operator ProjectionFutureId(Guid value) => new(value);

    /// <summary>
    /// Creates a new <see cref="ProjectionFutureId"/> with a unique value.
    /// </summary>
    /// <returns>A new <see cref="ProjectionFutureId"/>.</returns>
    public static ProjectionFutureId New() => new(Guid.NewGuid());
}
