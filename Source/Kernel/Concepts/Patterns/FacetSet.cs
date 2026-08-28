// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Chronicle.Concepts.Patterns;

/// <summary>
/// Represents an itemset of facets - the combination of contextual dimensions a pattern is expressed in.
/// </summary>
/// <remarks>
/// A set is canonical: facets are ordered by name, at most one facet per name survives, and facets without a value
/// are dropped. Two sets built from the same facts therefore carry the same <see cref="Key"/> and compare equal,
/// which is what lets a mined pattern be counted once no matter which order its facets were extracted in.
/// </remarks>
public sealed record FacetSet
{
    /// <summary>
    /// Represents the empty <see cref="FacetSet"/> - the set matching every context.
    /// </summary>
    public static readonly FacetSet Empty = new([]);

    /// <summary>
    /// Initializes a new instance of the <see cref="FacetSet"/> class.
    /// </summary>
    /// <param name="facets">The <see cref="Facet">facets</see> the set is made of.</param>
    public FacetSet(IEnumerable<Facet> facets)
    {
        Facets = [.. facets
            .Where(facet => facet.Value.IsSpecified)
            .GroupBy(facet => facet.Name)
            .Select(group => group.First())
            .OrderBy(facet => facet.Name.Value, StringComparer.Ordinal)
            .ThenBy(facet => facet.Value.Value, StringComparer.Ordinal)];

        Key = BuildKey(Facets);
    }

    /// <summary>
    /// Gets the facets the set is made of, ordered canonically.
    /// </summary>
    public IReadOnlyList<Facet> Facets { get; }

    /// <summary>
    /// Gets the canonical <see cref="FacetSetKey"/> identifying the set.
    /// </summary>
    public FacetSetKey Key { get; }

    /// <summary>
    /// Gets how specific the set is - the number of facets it constrains.
    /// </summary>
    public int Specificity => Facets.Count;

    /// <summary>
    /// Gets a value indicating whether the set constrains nothing.
    /// </summary>
    public bool IsEmpty => Facets.Count == 0;

    /// <summary>
    /// Creates a <see cref="FacetSet"/> from name and value pairs.
    /// </summary>
    /// <param name="facets">The pairs to create from.</param>
    /// <returns>A new <see cref="FacetSet"/>.</returns>
    public static FacetSet From(IEnumerable<KeyValuePair<FacetName, FacetValue>> facets) =>
        new(facets.Select(pair => new Facet(pair.Key, pair.Value)));

    /// <summary>
    /// Creates a copy of the set with a facet added or replaced.
    /// </summary>
    /// <param name="name">The <see cref="FacetName"/> to set.</param>
    /// <param name="value">The <see cref="FacetValue"/> to set it to.</param>
    /// <returns>A new <see cref="FacetSet"/>.</returns>
    /// <remarks>
    /// Replacing rather than adding: a set holds at most one value per facet, and silently keeping whichever one
    /// arrived first would make a context built up in steps depend on the order it was written in.
    /// </remarks>
    public FacetSet With(FacetName name, FacetValue value) =>
        new(Facets.Where(facet => facet.Name != name).Append(new Facet(name, value)));

    /// <summary>
    /// Gets the value of a specific facet.
    /// </summary>
    /// <param name="name">The <see cref="FacetName"/> to get the value for.</param>
    /// <returns>The <see cref="FacetValue"/>, or <see cref="FacetValue.Unspecified"/> when the set does not constrain it.</returns>
    public FacetValue ValueOf(FacetName name) =>
        Facets.FirstOrDefault(facet => facet.Name == name)?.Value ?? FacetValue.Unspecified;

    /// <summary>
    /// Check whether the set constrains a specific facet.
    /// </summary>
    /// <param name="name">The <see cref="FacetName"/> to check for.</param>
    /// <returns>True when the set constrains it, false when not.</returns>
    public bool Constrains(FacetName name) => Facets.Any(facet => facet.Name == name);

    /// <summary>
    /// Check whether every facet in this set is also in another set.
    /// </summary>
    /// <param name="other">The <see cref="FacetSet"/> to check against.</param>
    /// <returns>True when this set is a subset of the other, false when not.</returns>
    public bool IsSubsetOf(FacetSet other) => Facets.All(other.Facets.Contains);

    /// <summary>
    /// Gets the set as a dictionary of names to values.
    /// </summary>
    /// <returns>A dictionary holding every facet in the set.</returns>
    public IReadOnlyDictionary<FacetName, FacetValue> AsDictionary() =>
        Facets.ToDictionary(facet => facet.Name, facet => facet.Value);

    /// <inheritdoc/>
    public bool Equals(FacetSet? other) => other is not null && Key == other.Key;

    /// <inheritdoc/>
    public override int GetHashCode() => Key.GetHashCode();

    /// <inheritdoc/>
    public override string ToString() => Key.Value;

    static FacetSetKey BuildKey(IReadOnlyList<Facet> facets)
    {
        if (facets.Count == 0)
        {
            return FacetSetKey.Empty;
        }

        var builder = new StringBuilder();
        for (var index = 0; index < facets.Count; index++)
        {
            if (index > 0)
            {
                builder.Append(';');
            }

            Escape(builder, facets[index].Name.Value);
            builder.Append('=');
            Escape(builder, facets[index].Value.Value);
        }

        return new(builder.ToString());
    }

    static void Escape(StringBuilder builder, string value)
    {
        foreach (var character in value)
        {
            if (character is '\\' or ';' or '=')
            {
                builder.Append('\\');
            }

            builder.Append(character);
        }
    }
}
