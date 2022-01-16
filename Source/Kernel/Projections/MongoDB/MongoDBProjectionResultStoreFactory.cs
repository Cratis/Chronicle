// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Configuration;
using Cratis.Execution;
using Cratis.Extensions.MongoDB;

namespace Cratis.Events.Projections.MongoDB
{
    /// <summary>
    /// Represents an implementation of <see cref="IProjectionResultStoreFactory"/>.
    /// </summary>
    public class MongoDBProjectionResultStoreFactory : IProjectionResultStoreFactory
    {
        readonly IMongoDBClientFactory _clientFactory;
        readonly IExecutionContextManager _executionContextManager;
        readonly Storage _configuration;

        /// <inheritdoc/>
        public ProjectionResultStoreTypeId TypeId => MongoDBProjectionResultStore.ProjectionResultStoreTypeId;

        /// <summary>
        /// /// Initializes a new instance of the <see cref="MongoDBProjectionResultStoreFactory"/> class.
        /// </summary>
        /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with execution context.</param>
        /// <param name="clientFactory"><see cref="IMongoDBClientFactory"/> to use.</param>
        /// <param name="configuration"><see cref="Storage"/> configuration.</param>
        public MongoDBProjectionResultStoreFactory(
            IExecutionContextManager executionContextManager,
            IMongoDBClientFactory clientFactory,
            Storage configuration)
        {
            _clientFactory = clientFactory;
            _executionContextManager = executionContextManager;
            _configuration = configuration;
        }

        /// <inheritdoc/>
        public IProjectionResultStore CreateFor(Model model) => new MongoDBProjectionResultStore(model, _executionContextManager, _clientFactory, _configuration);
    }
}
