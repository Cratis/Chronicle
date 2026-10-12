// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Concepts.Observation.Sequencing;

/// <summary>
/// Describes a canonical key for display without pretending a digest is reversible text.
/// </summary>
public sealed record SequencingKeyDescription
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SequencingKeyDescription"/> class.
    /// </summary>
    /// <param name="kind">The key domain.</param>
    /// <param name="parts">The value parts.</param>
    /// <param name="text">The unresolved text, if present.</param>
    /// <param name="digestHex">The event source id or unresolved UTF-16 digest, if present.</param>
    internal SequencingKeyDescription(SequencingKeyKind kind, ImmutableArray<string> parts, string? text, string? digestHex)
    {
        Kind = kind;
        Parts = parts;
        Text = text;
        DigestHex = digestHex;
    }

    /// <summary>
    /// Gets the canonical key domain.
    /// </summary>
    public SequencingKeyKind Kind { get; }

    /// <summary>
    /// Gets the value key parts, or an empty array for an unresolved or event source id digest key.
    /// </summary>
    public ImmutableArray<string> Parts { get; }

    /// <summary>
    /// Gets the unresolved event type text, if stored in text form.
    /// </summary>
    public string? Text { get; }

    /// <summary>
    /// Gets the lowercase UTF-16 digest hex, if stored in digest form.
    /// </summary>
    public string? DigestHex { get; }

    /// <inheritdoc/>
    public bool Equals(SequencingKeyDescription? other) => other is not null && Kind == other.Kind
        && Parts.SequenceEqual(other.Parts, StringComparer.Ordinal)
        && StringComparer.Ordinal.Equals(Text, other.Text) && StringComparer.Ordinal.Equals(DigestHex, other.DigestHex);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = default(HashCode);
        hash.Add(Kind);
        foreach (var part in Parts)
        {
            hash.Add(part, StringComparer.Ordinal);
        }

        hash.Add(Text, StringComparer.Ordinal);
        hash.Add(DigestHex, StringComparer.Ordinal);

        return hash.ToHashCode();
    }
}
