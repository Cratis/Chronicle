// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.SequenceQueries;

/// <summary>
/// Represents the unique identifier of a folder in the saved query hierarchy.
/// </summary>
/// <param name="Value">The underlying value.</param>
/// <remarks>
/// A folder carries an identity of its own rather than being known by its path, so that renaming or
/// moving one is an update rather than a delete followed by an insert.
/// </remarks>
public record SequenceQueryFolderId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Represents an unset <see cref="SequenceQueryFolderId"/>.
    /// </summary>
    public static readonly SequenceQueryFolderId NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="SequenceQueryFolderId"/>.
    /// </summary>
    /// <param name="value">Value to convert from.</param>
    public static implicit operator SequenceQueryFolderId(string value) => new(value);

    /// <summary>
    /// Create a new unique <see cref="SequenceQueryFolderId"/>.
    /// </summary>
    /// <returns>A new <see cref="SequenceQueryFolderId"/>.</returns>
    public static SequenceQueryFolderId New() => new(Guid.NewGuid().ToString());
}
