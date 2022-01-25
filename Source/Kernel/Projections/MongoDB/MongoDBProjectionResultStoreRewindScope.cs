// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Aksio.Cratis.Events.Projections.MongoDB
{
    /// <summary>
    /// Represents an implementation of <see cref="IProjectionResultStoreRewindScope"/> for MongoDB.
    /// </summary>
    public class MongoDBProjectionResultStoreRewindScope : IProjectionResultStoreRewindScope
    {
        readonly IMongoDatabase _database;
        readonly Action _onDispose;

        /// <inheritdoc/>
        public Model Model { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDBProjectionResultStoreRewindScope"/> class.
        /// </summary>
        /// <param name="database"><see cref="IMongoDatabase"/>.</param>
        /// <param name="model"><see cref="Model"/> the scope is for.</param>
        /// <param name="onDispose"><see cref="Action"/> to call when scope is disposed.</param>
        public MongoDBProjectionResultStoreRewindScope(
            IMongoDatabase database,
            Model model,
            Action onDispose)
        {
            _database = database;
            Model = model;
            _onDispose = onDispose;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            var rewindName = MongoDBProjectionResultStore.GetRewindCollectionName(Model.Name);
            var rewoundCollectionsPrefix = $"{Model.Name}-";
            var collectionNames = _database.ListCollectionNames().ToList();
            var nextCollectionSequenceNumber = 1;
            var rewoundCollectionNames = collectionNames.Where(_ => _.StartsWith(rewoundCollectionsPrefix, StringComparison.InvariantCulture)).ToArray();
            if (rewoundCollectionNames.Length > 0)
            {
                nextCollectionSequenceNumber = rewoundCollectionNames
                    .Select(_ =>
                    {
                        var postfix = _.Substring(rewoundCollectionsPrefix.Length);
                        if (int.TryParse(postfix, out var value))
                        {
                            return value;
                        }
                        return -1;
                    })
                    .Where(_ => _ >= 0)
                    .OrderByDescending(_ => _)
                    .First() + 1;
            }
            var oldCollectionName = $"{rewoundCollectionsPrefix}{nextCollectionSequenceNumber}";

            if (collectionNames.Contains(Model.Name))
            {
                _database.RenameCollection(Model.Name, oldCollectionName);
            }

            if (collectionNames.Contains(rewindName))
            {
                _database.RenameCollection(rewindName, Model.Name);
            }

            _onDispose();

            GC.SuppressFinalize(this);
        }
    }
}
