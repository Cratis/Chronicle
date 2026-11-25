// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// Represents the revision of an observer's definition: how many times its content has changed since it was first stored.
/// </summary>
/// <remarks>
/// A silo compares the revision it holds with the one in storage to know whether it must read the definition again, so a
/// change is replicated by revision rather than by invalidating a cache. A definition stored before revisions existed has
/// <see cref="None"/>, and the first change after that makes it one.
/// </remarks>
/// <param name="Value">The revision value.</param>
public record ObserverDefinitionRevision(long Value) : ConceptAs<long>(Value)
{
    /// <summary>
    /// Gets the revision of a definition that has not been revised, including one stored before revisions existed.
    /// </summary>
    public static readonly ObserverDefinitionRevision None = 0;

    /// <summary>
    /// Converts a <see cref="long"/> to an <see cref="ObserverDefinitionRevision"/>.
    /// </summary>
    /// <param name="value">The revision value.</param>
    /// <returns>The <see cref="ObserverDefinitionRevision"/>.</returns>
    public static implicit operator ObserverDefinitionRevision(long value) => new(value);

    /// <summary>
    /// Advances to the next revision.
    /// </summary>
    /// <returns>The next <see cref="ObserverDefinitionRevision"/>.</returns>
    public ObserverDefinitionRevision Next() => new(Value + 1);
}
