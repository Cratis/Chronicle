// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Compliance;
using Aksio.Cratis.Compliance.MongoDB;
using Aksio.Cratis.Events.Projections.Changes;
using Aksio.Cratis.Events.Projections.Definitions;
using Aksio.Cratis.Events.Projections.MongoDB;
using Aksio.Cratis.Events.Schemas;
using Aksio.Cratis.Events.Schemas.MongoDB;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Events.Store.MongoDB;
using Aksio.Cratis.Events.Store.MongoDB.Observation;
using Aksio.Cratis.Events.Store.Observation;
using Aksio.Cratis.Extensions.Autofac;
using Autofac;

namespace Aksio.Cratis.Server;

/// <summary>
/// Represents an Autofac <see cref="Module"/> for configuring service registrations for the server.
/// </summary>
public class ServiceRegistrations : Module
{
    /// <inheritdoc/>
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<MongoDBEncryptionKeyStore>().AsSelf().InstancePerMicroservice();
        builder.Register(_ => new CacheEncryptionKeyStore(_.Resolve<MongoDBEncryptionKeyStore>())).As<IEncryptionKeyStore>().InstancePerMicroservice();
        builder.RegisterType<MongoDBChangesetStorage>().As<IChangesetStorage>().InstancePerMicroserviceAndTenant();
        builder.RegisterType<MongoDBSchemaStore>().As<ISchemaStore>().InstancePerMicroservice();
        builder.RegisterType<MongoDBProjectionDefinitionsStorage>().As<IProjectionDefinitionsStorage>().InstancePerMicroservice();
        builder.RegisterType<MongoDBProjectionPipelineDefinitionsStorage>().As<IProjectionPipelineDefinitionsStorage>().InstancePerMicroservice();
        builder.RegisterType<MongoDBProjectionDefinitionsStorage>().As<IProjectionDefinitionsStorage>().InstancePerMicroservice();
        builder.RegisterType<MongoDBEventSequenceStorageProvider>().As<IEventSequenceStorageProvider>().SingleInstance();
        builder.RegisterType<MongoDBEventSequences>().As<IEventSequences>().SingleInstance();
        builder.RegisterType<MongoDBObserversState>().As<IObserversState>().SingleInstance();
    }
}
