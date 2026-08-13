// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.SequenceQueries;

/// <summary>
/// Represents the folder a saved event sequence query is filed under.
/// </summary>
/// <param name="Value">The underlying value.</param>
/// <remarks>
/// A forward-slash separated path relative to the scope the query belongs to, so
/// <c>Diagnostics/Failures</c> nests one folder inside another. An empty value files the query
/// directly under its scope rather than in any folder, which is why the root is a sentinel rather
/// than a nullable value.
/// </remarks>
public record SequenceQueryFolder(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Represents a query that sits directly under its scope rather than in a folder.
    /// </summary>
    public static readonly SequenceQueryFolder Root = new(string.Empty);

    /// <summary>
    /// The character separating one folder from the next within the path.
    /// </summary>
    public const char Separator = '/';

    /// <summary>
    /// Gets the individual folder names the path is made of, outermost first.
    /// </summary>
    public IEnumerable<string> Segments =>
        Value.Split(Separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="SequenceQueryFolder"/>.
    /// </summary>
    /// <param name="value">Value to convert from.</param>
    public static implicit operator SequenceQueryFolder(string value) => new(value);
}
