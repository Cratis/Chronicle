// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Generations.Enablement;

/// <summary>
/// Joins existing definitions and observer evidence without modifying physical data.
/// </summary>
/// <param name="storage">The durable store.</param>
public sealed class LegacyMaterializations(IStorage storage) : ILegacyMaterializations
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<LegacyMaterialization>> GetAll(EventStoreName eventStore)
    {
        var store = storage.GetEventStore(eventStore);
        var projections = (await store.Projections.GetAll()).ToArray();
        var models = (await store.ReadModels.GetAll()).ToDictionary(model => model.Identifier);
        var result = new List<LegacyMaterialization>();
        foreach (var item in await store.Namespaces.GetAll())
        {
            var observers = (await store.GetNamespace(item.Name).Observers.GetAll()).ToDictionary(observer => observer.Identifier.Value);
            foreach (var projection in projections.Where(projection => projection.IsActive))
            {
                if (!models.TryGetValue(projection.ReadModel, out var model))
                {
                    throw new InvalidOperationException($"Projection '{projection.Identifier}' has no read model definition.");
                }
                if (!observers.TryGetValue(projection.Identifier.Value, out var observer))
                {
                    throw new InvalidOperationException($"Projection '{projection.Identifier}' has no observer state in '{item.Name}'.");
                }
                result.Add(new(projection.Identifier, item.Name, model.ContainerName, observer.Identifier, observer.LastHandledEventSequenceNumber.ToString(), observer.FailedPartitionCount.Value));
            }
        }
        return result.OrderBy(item => item.Namespace.Value, StringComparer.Ordinal).ThenBy(item => item.Projection.Value, StringComparer.Ordinal).ToArray();
    }
}
