// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reactions;

/// <summary>
/// Represents the unique identifier of a reducer.
/// </summary>
/// <param name="value">The actual value.</param>
public record ReactionId(string value) : ConceptAs<string>(value)
{
    /// <summary>
    /// Gets the representation of an unspecified <see cref="ReactionId"/>.
    /// </summary>
    public static readonly ReactionId Unspecified = ObserverId.Unspecified;

    /// <summary>
    /// Implicitly convert from a string to <see cref="ReactionId"/>.
    /// </summary>
    /// <param name="id">String to convert from.</param>
    public static implicit operator ReactionId(string id) => new(id);

    /// <summary>
    /// Implicitly convert from <see cref="ReactionId"/> to <see cref="ObserverId"/>.
    /// </summary>
    /// <param name="id"><see cref="ReactionId"/> to convert from.</param>
    public static implicit operator ObserverId(ReactionId id) => new(id.Value);

    /// <summary>
    /// Implicitly convert from <see cref="ObserverId"/> to <see cref="ReactionId"/>.
    /// </summary>
    /// <param name="id"><see cref="ObserverId"/> to convert from.</param>
    public static implicit operator ReactionId(ObserverId id) => new(id.Value);
}
