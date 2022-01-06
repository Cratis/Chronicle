// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Configuration;
using Cratis.Extensions.MongoDB;

namespace Cratis.Events.Projections.MongoDB
{
    /// <summary>
    /// Represents an implementation of <see cref="IProjectionResultStoreFactory"/>.
    /// </summary>
    public class MongoDBProjectionResultStoreFactory : IProjectionResultStoreFactory
    {
        readonly IMongoDBClientFactory _clientFactory;
        readonly Storage _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDBProjectionResultStoreFactory"/> class.
        /// </summary>
        /// <param name="clientFactory"><see cref="IMongoDBClientFactory"/> to use.</param>
        /// <param name="configuration"><see cref="Storage"/> configuration.</param>
        public MongoDBProjectionResultStoreFactory(IMongoDBClientFactory clientFactory, Storage configuration)
        {
            _clientFactory = clientFactory;
            _configuration = configuration;
        }

        /// <inheritdoc/>
        public ProjectionResultStoreTypeId TypeId => MongoDBProjectionResultStore.ProjectionResultStoreTypeId;

        /// <inheritdoc/>
        public IProjectionResultStore CreateFor(Model model) => new MongoDBProjectionResultStore(model, _clientFactory, _configuration);
    }
}
