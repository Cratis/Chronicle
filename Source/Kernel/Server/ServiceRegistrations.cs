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
using Aksio.Cratis.Kernel.Keys;
using Aksio.Cratis.Kernel.MongoDB.EventSequences;
using Aksio.Cratis.Kernel.MongoDB.Identities;
using Aksio.Cratis.Kernel.MongoDB.Jobs;
using Aksio.Cratis.Kernel.MongoDB.Keys;
using Aksio.Cratis.Kernel.MongoDB.Observation;
using Aksio.Cratis.Kernel.MongoDB.Projections;
using Aksio.Cratis.Kernel.Persistence.Jobs;
using Aksio.Cratis.Kernel.Persistence.Observation;
using Aksio.Cratis.Kernel.Persistence.Observation.Replaying;
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
        builder.RegisterType<MongoDBSchemaStore>().As<Schemas.ISchemaStore>().InstancePerMicroservice();
        builder.RegisterType<MongoDBProjectionDefinitionsStorage>().As<IProjectionDefinitionsStorage>().InstancePerMicroservice();
        builder.RegisterType<MongoDBProjectionPipelineDefinitionsStorage>().As<IProjectionPipelineDefinitionsStorage>().InstancePerMicroservice();
        builder.RegisterType<MongoDBProjectionDefinitionsStorage>().As<IProjectionDefinitionsStorage>().InstancePerMicroservice();

        builder.RegisterType<MongoDBChangesetStorage>().As<IChangesetStorage>().InstancePerMicroserviceAndTenant();
        builder.RegisterType<MongoDBEventSequenceStorage>().As<IEventSequenceStorage>().InstancePerMicroserviceAndTenant();
        builder.RegisterType<MongoDBObserverStorage>().As<IObserverStorage>().InstancePerMicroserviceAndTenant();
        builder.RegisterType<MongoDBFailedPartitionStorage>().As<IFailedPartitionsStorage>().InstancePerMicroserviceAndTenant();
        builder.RegisterType<MongoDBObserverKeyIndexes>().As<IObserverKeyIndexes>().InstancePerMicroserviceAndTenant();
        builder.RegisterType(typeof(MongoDBJobStorage)).As(typeof(IJobStorage)).InstancePerMicroserviceAndTenant();
        builder.RegisterType(typeof(MongoDBJobStepStorage)).As(typeof(IJobStepStorage)).InstancePerMicroserviceAndTenant();
        builder.RegisterGeneric(typeof(MongoDBJobStorage<>)).As(typeof(IJobStorage<>)).InstancePerMicroserviceAndTenant();
        builder.RegisterGeneric(typeof(MongoDBJobStepStorage<>)).As(typeof(IJobStepStorage<>)).InstancePerMicroserviceAndTenant();
        builder.RegisterType(typeof(MongoDBReplayCandidateStorage)).As(typeof(IReplayCandidatesStorage)).InstancePerMicroserviceAndTenant();

        builder.RegisterType<MongoDBIdentityStore>().As<IIdentityStore>().InstancePerTenant();
    }
}
