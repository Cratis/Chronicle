// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Coordinates self-model ownership checks and publication through one store-local monitor.
/// </summary>
internal static class ReadModelBuilderOwnership
{
    /// <summary>
    /// Gets the store-local monitor, sharing native registries on custom stores as well.
    /// </summary>
    /// <param name="store">The owning store.</param>
    /// <param name="fallback">The registry's fallback monitor.</param>
    /// <returns>The shared ownership monitor.</returns>
    internal static object SyncFor(IEventStore store, object fallback) => store switch
    {
        EventStore native => native.ReadModelOwnershipSync,
        _ when store.Reducers is Reducers.Reducers reducers => reducers.ReadModelOwnershipSync,
        _ when store.Projections is Projections.Projections projections => projections.ReadModelOwnershipSync,
        _ => fallback
    };

    /// <summary>
    /// Rejects conflicting ownership only when a non-classic self model is involved.
    /// </summary>
    /// <param name="store">The owning store.</param>
    /// <param name="artifacts">The owning declarations.</param>
    /// <param name="catalog">The reducer descriptor resolver.</param>
    /// <param name="model">The maintained model.</param>
    /// <param name="builder">The proposed builder.</param>
    /// <param name="isSelf">Whether the proposed builder is self reduction.</param>
    /// <exception cref="AmbiguousReadModelBuilder">The self model already has another builder.</exception>
    internal static void Validate(IEventStore store, IClientArtifactsProvider artifacts, ReducerCatalog catalog, Type model, Type builder, bool isSelf)
    {
        var selfHandler = store.Reducers?.GetAllHandlers().FirstOrDefault(handler => handler.ReadModelType == model && ReducerCatalog.IsSelfReducer(handler.ReducerType));
        if (!isSelf && !ReducerCatalog.IsSelfReducer(model) && selfHandler is null)
        {
            return;
        }

        var conflicts = new List<Type>();
        if (!isSelf || model.IsAssignableTo(typeof(IReducer)) || model.IsAssignableTo(typeof(Reactors.IReactor)) || model.HasGenuineModelBoundProjectionAttributes())
        {
            conflicts.Add(builder);
        }
        conflicts.AddRange(artifacts.Projections.Where(type => ProjectionTypeExtensions.GetReadModelType(type) == model));
        conflicts.AddRange(artifacts.ModelBoundProjections.Where(type => type == model));
        conflicts.AddRange(ExplicitArtifactCatalog.From(artifacts)?.ProjectionReadModelTypes.Where(type => type == model) ?? []);
        conflicts.AddRange(artifacts.Reducers.Where(type => type != builder && catalog.IsDiscoverable(type) && catalog.Get(type).ReadModelType == model));
        conflicts.AddRange(store.Projections?.GetAllHandlers().Where(handler => handler.ReadModelType == model).Select(handler => handler.ReadModelType) ?? []);
        conflicts.AddRange(store.Reducers?.GetAllHandlers().Where(handler => handler.ReadModelType == model && handler.ReducerType != builder).Select(handler => handler.ReducerType) ?? []);
        if (conflicts.Count > 0)
        {
            throw new AmbiguousReadModelBuilder(model, conflicts.Prepend(model));
        }
    }
}
