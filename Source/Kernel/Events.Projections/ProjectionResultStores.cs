// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Types;

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Represents an implementation of <see cref="IProjectionResultStores"/>.
    /// </summary>
    public class ProjectionResultStores : IProjectionResultStores
    {
        readonly IDictionary<ProjectionResultStoreTypeId, IProjectionResultStore> _stores;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectionResultStores"/> class.
        /// </summary>
        /// <param name="stores"><see cref="IInstancesOf{T}"/> of <see cref="IProjectionResultStore"/>.</param>
        public ProjectionResultStores(IInstancesOf<IProjectionResultStore> stores)
        {
            _stores = stores.ToDictionary(_ => _.TypeId, _ => _);
        }

        /// <inheritdoc/>
        public IProjectionResultStore GetForType(ProjectionResultStoreTypeId typeId)
        {
            ThrowIfUnknownProjectionResultStore(typeId);
            return _stores[typeId];
        }

        /// <inheritdoc/>
        public bool HasType(ProjectionResultStoreTypeId typeId) => _stores.ContainsKey(typeId);

        void ThrowIfUnknownProjectionResultStore(ProjectionResultStoreTypeId typeId)
        {
            if (!HasType(typeId)) throw new UnknownProjectionResultStore(typeId);
        }
    }
}
