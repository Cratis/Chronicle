// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Aksio.Cratis.Types;

namespace Aksio.Cratis.Events.Projections
{
    /// <summary>
    /// Represents an implementation of <see cref="IProjectionResultStoreFactory"/>.
    /// </summary>
    public class ProjectionResultStores : IProjectionResultStores
    {
        record Key(ProjectionResultStoreTypeId TypeId, Model Model);

        readonly IDictionary<ProjectionResultStoreTypeId, IProjectionResultStoreFactory> _factories;
        readonly ConcurrentDictionary<Key, IProjectionResultStore> _stores = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectionResultStores"/> class.
        /// </summary>
        /// <param name="stores"><see cref="IInstancesOf{T}"/> of <see cref="IProjectionResultStoreFactory"/>.</param>
        public ProjectionResultStores(IInstancesOf<IProjectionResultStoreFactory> stores)
        {
            _factories = stores.ToDictionary(_ => _.TypeId, _ => _);
        }

        /// <inheritdoc/>
        public IProjectionResultStore GetForTypeAndModel(ProjectionResultStoreTypeId typeId, Model model)
        {
            ThrowIfUnknownProjectionResultStore(typeId);
            var key = new Key(typeId, model);
            if (_stores.ContainsKey(key)) return _stores[key];
            return _stores[key] = _factories[typeId].CreateFor(model);
        }

        /// <inheritdoc/>
        public bool HasType(ProjectionResultStoreTypeId typeId) => _factories.ContainsKey(typeId);

        void ThrowIfUnknownProjectionResultStore(ProjectionResultStoreTypeId typeId)
        {
            if (!HasType(typeId)) throw new UnknownProjectionResultStore(typeId);
        }
    }
}
