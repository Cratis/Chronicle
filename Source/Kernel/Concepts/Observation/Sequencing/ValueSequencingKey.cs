// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Concepts.Observation.Sequencing;

/// <summary>
/// Represents an ordinal tuple of one to sixteen sequencing key parts.
/// </summary>
public sealed record ValueSequencingKey : SequencingKey
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValueSequencingKey"/> class.
    /// </summary>
    /// <param name="parts">The immutable text parts.</param>
    /// <exception cref="InvalidSequencingKey">The parts are missing, null, or outside the supported count.</exception>
    public ValueSequencingKey(ImmutableArray<string> parts)
    {
        if (parts.IsDefault || parts.Length is < 1 or > 16 || parts.Any(part => part is null))
        {
            throw new InvalidSequencingKey("A value key requires one to sixteen non-null parts.");
        }

        Parts = ImmutableArray.Create(parts.ToArray());
    }

    /// <summary>
    /// Gets the ordinal parts of the value key.
    /// </summary>
    public ImmutableArray<string> Parts { get; }

    /// <inheritdoc/>
    public bool Equals(ValueSequencingKey? other) => other is not null && Parts.SequenceEqual(other.Parts, StringComparer.Ordinal);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = default(HashCode);
        foreach (var part in Parts)
        {
            hash.Add(part, StringComparer.Ordinal);
        }

        return hash.ToHashCode();
    }
}
