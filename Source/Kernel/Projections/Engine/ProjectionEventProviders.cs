// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Types;

namespace Aksio.Cratis.Events.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionResultStores"/>.
/// </summary>
public class ProjectionEventProviders : IProjectionEventProviders
{
    readonly IDictionary<ProjectionEventProviderTypeId, IProjectionEventProvider> _stores;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionEventProviders"/> class.
    /// </summary>
    /// <param name="stores"><see cref="IInstancesOf{T}"/> of <see cref="IProjectionResultStore"/>.</param>
    public ProjectionEventProviders(IInstancesOf<IProjectionEventProvider> stores)
    {
        _stores = stores.ToDictionary(_ => _.TypeId, _ => _);
    }

    /// <inheritdoc/>
    public IProjectionEventProvider GetForType(ProjectionEventProviderTypeId typeId)
    {
        ThrowIfUnknownProjectionEventProvider(typeId);
        return _stores[typeId];
    }

    /// <inheritdoc/>
    public bool HasType(ProjectionEventProviderTypeId typeId) => _stores.ContainsKey(typeId);

    void ThrowIfUnknownProjectionEventProvider(ProjectionEventProviderTypeId typeId)
    {
        if (!HasType(typeId)) throw new UnknownProjectionEventProvider(typeId);
    }
}
