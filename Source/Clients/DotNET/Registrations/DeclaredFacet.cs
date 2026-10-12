// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations;

/// <summary>
/// One supplied facet with its declaration provenance, before defaults are applied.
/// </summary>
/// <typeparam name="T">The facet's value type.</typeparam>
/// <param name="Value">The supplied value.</param>
/// <param name="Source">The declaration source.</param>
internal sealed record DeclaredFacet<T>(T Value, string Source)
{
    /// <summary>
    /// Merges equal peers or rejects conflicting supplied values.
    /// </summary>
    /// <param name="type">The artifact type.</param>
    /// <param name="facet">The facet name.</param>
    /// <param name="first">The earlier declaration, if any.</param>
    /// <param name="second">The later declaration, if any.</param>
    /// <returns>The coalesced declaration.</returns>
    /// <exception cref="ConflictingArtifactDeclaration">The supplied values differ.</exception>
    internal static DeclaredFacet<T>? Merge(Type type, string facet, DeclaredFacet<T>? first, DeclaredFacet<T>? second)
    {
        if (first is null) return second;
        if (second is null) return first;
        if (!EqualityComparer<T>.Default.Equals(first.Value, second.Value))
        {
            throw new ConflictingArtifactDeclaration(type, facet, first.Source, $"{first.Value}", second.Source, $"{second.Value}");
        }

        return first;
    }
}
