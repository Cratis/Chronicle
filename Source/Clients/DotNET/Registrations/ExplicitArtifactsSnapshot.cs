// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Registrations;

/// <summary>
/// The options declarations captured together at store construction, independent of later options edits.
/// </summary>
/// <param name="EventTypes">The event declarations.</param>
/// <param name="Types">The other type nominations.</param>
/// <param name="Projections">The projection recipes.</param>
/// <param name="Reactors">The delegate reactor recipes.</param>
internal sealed record ExplicitArtifactsSnapshot(
    ImmutableArray<EventTypeRegistration> EventTypes,
    ImmutableArray<ExplicitArtifactRegistration> Types,
    ImmutableArray<IExplicitProjection> Projections,
    ImmutableArray<(ReactorId Id, Action<IReactorBuilder> Define)> Reactors)
{
    /// <summary>
    /// Preserves empty options or merges nominated artifacts into a store-local catalog.
    /// </summary>
    /// <param name="discovered">The native discovery provider for this preparation revision.</param>
    /// <returns>The original provider for empty options, otherwise the immutable catalog for this revision.</returns>
    internal IClientArtifactsProvider ApplyTo(IClientArtifactsProvider discovered) =>
        EventTypes.IsEmpty && Types.IsEmpty && Projections.IsEmpty && Reactors.IsEmpty ? discovered : new ExplicitArtifactCatalog(discovered, this);

    /// <summary>
    /// Applies the captured projection recipes before discovery.
    /// </summary>
    /// <param name="projections">The store's projections.</param>
    internal void ApplyTo(Projections.Projections projections)
    {
        foreach (var projection in Projections)
        {
            projections.Add(projection);
        }
    }

    /// <summary>
    /// Applies the captured delegate reactor recipes before discovery.
    /// </summary>
    /// <param name="reactors">The store's reactors.</param>
    internal void ApplyTo(Reactors.Reactors reactors)
    {
        foreach (var (id, define) in Reactors)
        {
            reactors.Add(id, define);
        }
    }
}
