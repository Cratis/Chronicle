// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Defines the storage for <see cref="IProjection">projections</see>.
    /// </summary>
    public interface IProjectionStorage
    {
        /// <summary>
        /// Find a model by key, or return an empty object if not found.
        /// </summary>
        /// <param name="model"><see cref="IModel"/> to find for.</param>
        /// <param name="key">Key of the model to find.</param>
        /// <returns>A model instance with the data from the source, or an empty object.</returns>
        Task<ExpandoObject> FindOrDefault(IModel model, object key);

        /// <summary>
        /// Update or insert model based on key.
        /// </summary>
        /// <param name="model"><see cref="IModel"/> to apply for.</param>
        /// <param name="key">Key of the model to upsert.</param>
        /// <param name="changeset">All changes in the form of a <see cref="Changeset"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task ApplyChanges(IModel model, object key, Changeset changeset);
    }
}
