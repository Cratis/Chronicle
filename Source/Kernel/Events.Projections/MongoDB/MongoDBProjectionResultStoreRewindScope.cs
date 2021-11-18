// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections.MongoDB
{
    /// <summary>
    /// Represents an implementation of <see cref="IProjectionResultStoreRewindScope"/> for MongoDB.
    /// </summary>
    public class MongoDBProjectionResultStoreRewindScope : IProjectionResultStoreRewindScope
    {
        readonly Action _onDispose;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDBProjectionResultStoreRewindScope"/> class.
        /// </summary>
        /// <param name="model"><see cref="Model"/> the scope is for.</param>
        /// <param name="onDispose"><see cref="Action"/> to call when scope is disposed.</param>
        public MongoDBProjectionResultStoreRewindScope(Model model, Action onDispose)
        {
            Model = model;
            _onDispose = onDispose;
        }

        /// <inheritdoc/>
        public Model Model { get; }

        /// <inheritdoc/>
        public void Dispose()
        {
            _onDispose();
        }
    }
}
