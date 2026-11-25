// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Finds retained producer evidence without relying on active-manager caches or identifier conventions.
/// </summary>
internal static class LegacyReadModelSinkInventory
{
    /// <summary>
    /// Reads current definitions and every discoverable projection history before any mutation can remove them.
    /// </summary>
    /// <param name="storage">The event-store storage.</param>
    /// <param name="includeUnresolved">Whether one-time legacy history and unresolved bindings must also be preserved.</param>
    /// <returns>The destinations requiring quarantine.</returns>
    internal static async Task<IReadOnlyList<ReadModelDefinition>> Find(IEventStoreStorage storage, bool includeUnresolved)
    {
        var models = (await storage.ReadModels.GetAll()).ToArray();
        var projections = (await storage.Projections.GetAll()).ToArray();
        var protectedModels = projections.Where(IsProtected).Select(_ => _.ReadModel).ToHashSet();
        if (includeUnresolved) await AddHistory(storage, models, projections, protectedModels);
        return models.Where(model => protectedModels.Contains(model.Identifier) ||
            (includeUnresolved && IsUnresolved(model, projections))).ToArray();
    }

    static bool IsProtected(ProjectionDefinition definition) => definition.UsesUnsupportedEventSequence();

    static bool IsUnresolved(ReadModelDefinition model, IEnumerable<ProjectionDefinition> projections) =>
        model.ObserverType == ReadModelObserverType.Projection &&
        !projections.Any(_ => _.Identifier.Value == model.ObserverIdentifier.Value);

    static async Task AddHistory(
        IEventStoreStorage storage,
        IEnumerable<ReadModelDefinition> models,
        IEnumerable<ProjectionDefinition> projections,
        HashSet<ReadModelIdentifier> protectedModels)
    {
        var identifiers = projections.Select(_ => _.Identifier.Value)
            .Concat(models.Where(_ => _.ObserverType == ReadModelObserverType.Projection).Select(_ => _.ObserverIdentifier.Value))
            .Distinct(StringComparer.Ordinal);
        foreach (var identifier in identifiers)
        {
            var history = await storage.ProjectionDefinitionHistory.GetFor((ProjectionId)identifier);
            protectedModels.UnionWith(history.Where(_ => IsProtected(_.Definition)).Select(_ => _.Definition.ReadModel));
        }
    }
}
