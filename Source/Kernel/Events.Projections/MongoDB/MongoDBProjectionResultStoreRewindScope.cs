// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Events.Projections.MongoDB
{
    /// <summary>
    /// Represents an implementation of <see cref="IProjectionResultStoreRewindScope"/> for MongoDB.
    /// </summary>
    public class MongoDBProjectionResultStoreRewindScope : IProjectionResultStoreRewindScope
    {
        readonly IMongoDatabase _database;
        readonly Action _onDispose;

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
        public Model Model { get; }

        /// <inheritdoc/>
        public void Dispose()
        {
            var rewindName = MongoDBProjectionResultStore.GetRewindCollectionName(Model.Name);
            var oldCollectionsPrefix = $"{Model.Name}-";
            var collectionNames = _database.ListCollectionNames().ToList();
            var nextCollectionSequenceNumber = 1;
            if (collectionNames.Any(_ => _.StartsWith(oldCollectionsPrefix, StringComparison.InvariantCulture)))
            {
                nextCollectionSequenceNumber = collectionNames
                    .Select(_ =>
                    {
                        var postfix = _.Substring(oldCollectionsPrefix.Length);
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
            var oldCollectionName = $"{oldCollectionsPrefix}{nextCollectionSequenceNumber}";

            _database.RenameCollection(Model.Name, oldCollectionName);
            _database.RenameCollection(rewindName, Model.Name);

            _onDispose();
        }
    }
}
