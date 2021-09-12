// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Autofac;
using Cratis.Configuration;
using MongoDB.Driver;

namespace Cratis.Events.Store.MongoDB
{
    /// <summary>
    /// Represents a <see cref="Module"/> for MongoDB.
    /// </summary>
    public class MongoDBModule : Module
    {
        /// <inheritdoc/>
        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(context => {
                var connectionString = context.Resolve<EventStoreConfigurationAs<string>>()();
                return new MongoClient(connectionString);
            }).As<IMongoClient>();

            builder.Register(context => {
                var connectionString = context.Resolve<EventStoreConfigurationAs<string>>()();
                var client = context.Resolve<IMongoClient>();
                var url = new MongoUrl(connectionString);
                return client.GetDatabase(url.DatabaseName);
            }).As<IMongoDatabase>();
        }
    }
}
