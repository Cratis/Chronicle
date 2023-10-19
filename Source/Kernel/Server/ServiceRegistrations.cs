// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Applications.Autofac;
using Aksio.Cratis.Compliance.MongoDB;
using Aksio.Cratis.Events.MongoDB.Schemas;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Identities;
using Aksio.Cratis.Kernel.Engines.Changes;
using Aksio.Cratis.Kernel.Engines.Compliance;
using Aksio.Cratis.Kernel.Engines.Projections.Definitions;
using Aksio.Cratis.Kernel.Grains.Observation;
using Aksio.Cratis.Kernel.MongoDB.EventSequences;
using Aksio.Cratis.Kernel.MongoDB.Identities;
using Aksio.Cratis.Kernel.MongoDB.Observation;
using Aksio.Cratis.Kernel.MongoDB.Projections;
using Aksio.Cratis.Kernel.Observation;
using Autofac;

namespace Aksio.Cratis.Kernel.Server;

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
        builder.RegisterType<MongoDBSchemaStore>().As<Schemas.ISchemaStore>().InstancePerMicroservice();
        builder.RegisterType<MongoDBProjectionDefinitionsStorage>().As<IProjectionDefinitionsStorage>().InstancePerMicroservice();
        builder.RegisterType<MongoDBProjectionPipelineDefinitionsStorage>().As<IProjectionPipelineDefinitionsStorage>().InstancePerMicroservice();
        builder.RegisterType<MongoDBProjectionDefinitionsStorage>().As<IProjectionDefinitionsStorage>().InstancePerMicroservice();
        builder.RegisterType<MongoDBEventSequenceStorage>().As<IEventSequenceStorage>().SingleInstance();
        builder.RegisterType<MongoDBObserverStorage>().As<IObserverStorage>().SingleInstance();
        builder.RegisterType<MongoDBObserversState>().As<IObserversState>().SingleInstance();
        builder.RegisterType<MongoDBFailedPartitionState>().As<IFailedPartitionsState>().SingleInstance();
        builder.RegisterType<MongoDBIdentityStore>().As<IIdentityStore>().InstancePerTenant();
    }
}
