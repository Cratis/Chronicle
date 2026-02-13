// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Concepts.Projections;

/// <summary>
/// Represents the unique identifier of a projection.
/// </summary>
/// <param name="Value">The value.</param>
public record ProjectionId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// The value representing an unset projection identifier.
    /// </summary>
    public static readonly ProjectionId Unspecified = ObserverId.Unspecified;

    /// <summary>
    /// Indicates whether the projection identifier represents a preview projection.
    /// </summary>
    public bool IsPreview => Value.StartsWith("preview");

    /// <summary>
    /// Implicitly convert from <see cref="Guid"/> to <see cref="ProjectionId"/>.
    /// </summary>
    /// <param name="value"><see cref="Guid"/> to convert from.</param>
    public static implicit operator ProjectionId(string value) => new(value);

    /// <summary>
    /// Implicitly convert from <see cref="ProjectionId"/> to <see cref="ObserverId"/>.
    /// </summary>
    /// <param name="id"><see cref="ProjectionId"/> to convert from.</param>
    public static implicit operator ObserverId(ProjectionId id) => new(id.Value);

    /// <summary>
    /// Implicitly convert from <see cref="ObserverId"/> to <see cref="ProjectionId"/>.
    /// </summary>
    /// <param name="id"><see cref="ObserverId"/> to convert from.</param>
    public static implicit operator ProjectionId(ObserverId id) => new(id.Value);

    /// <summary>
    /// Creates a new preview projection identifier.
    /// </summary>
    /// <returns>New preview <see cref="ProjectionId"/>.</returns>
    public static ProjectionId CreatePreviewId() => new($"preview-{Guid.NewGuid()}");
}
