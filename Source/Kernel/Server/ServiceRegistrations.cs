// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Applications.Autofac;
using Aksio.Cratis.Compliance.MongoDB;
using Aksio.Cratis.Events.MongoDB.EventTypes;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Identities;
using Aksio.Cratis.Kernel.Engines.Changes;
using Aksio.Cratis.Kernel.Engines.Compliance;
using Aksio.Cratis.Kernel.Projections.Definitions;
using Aksio.Cratis.Kernel.Keys;
using Aksio.Cratis.Kernel.MongoDB.EventSequences;
using Aksio.Cratis.Kernel.MongoDB.Identities;
using Aksio.Cratis.Kernel.MongoDB.Jobs;
using Aksio.Cratis.Kernel.MongoDB.Keys;
using Aksio.Cratis.Kernel.MongoDB.Observation;
using Aksio.Cratis.Kernel.MongoDB.Projections;
using Aksio.Cratis.Kernel.MongoDB.Recommendations;
using Aksio.Cratis.Kernel.Persistence.Jobs;
using Aksio.Cratis.Kernel.Persistence.Observation;
using Aksio.Cratis.Kernel.Persistence.Recommendations;
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
        builder.RegisterType<EncryptionKeyStore>().AsSelf().InstancePerMicroservice();
        builder.Register(_ => new CacheEncryptionKeyStore(_.Resolve<EncryptionKeyStore>())).As<IEncryptionKeyStore>().InstancePerMicroservice();
        builder.RegisterType<EventTypesStorage>().As<Schemas.ISchemaStore>().InstancePerMicroservice();
        builder.RegisterType<ProjectionDefinitionsStorage>().As<IProjectionDefinitionsStorage>().InstancePerMicroservice();
        builder.RegisterType<ProjectionPipelineDefinitionsStorage>().As<IProjectionPipelineDefinitionsStorage>().InstancePerMicroservice();
        builder.RegisterType<ProjectionDefinitionsStorage>().As<IProjectionDefinitionsStorage>().InstancePerMicroservice();

        builder.RegisterType<ChangesetStorage>().As<IChangesetStorage>().InstancePerMicroserviceAndTenant();
        builder.RegisterType<EventSequenceStorage>().As<IEventSequenceStorage>().InstancePerMicroserviceAndTenant();
        builder.RegisterType<ObserverStorage>().As<IObserverStorage>().InstancePerMicroserviceAndTenant();
        builder.RegisterType<FailedPartitionStorage>().As<IFailedPartitionsStorage>().InstancePerMicroserviceAndTenant();
        builder.RegisterType<ObserverKeyIndexes>().As<IObserverKeyIndexes>().InstancePerMicroserviceAndTenant();
        builder.RegisterType(typeof(JobStorage)).As(typeof(IJobStorage)).InstancePerMicroserviceAndTenant();
        builder.RegisterType(typeof(JobStepStorage)).As(typeof(IJobStepStorage)).InstancePerMicroserviceAndTenant();
        builder.RegisterGeneric(typeof(JobStorage<>)).As(typeof(IJobStorage<>)).InstancePerMicroserviceAndTenant();
        builder.RegisterGeneric(typeof(JobStepStorage<>)).As(typeof(IJobStepStorage<>)).InstancePerMicroserviceAndTenant();
        builder.RegisterType(typeof(RecommendationStorage)).As(typeof(IRecommendationStorage)).InstancePerMicroserviceAndTenant();

        builder.RegisterType<IdentityStorage>().As<IIdentityStore>().InstancePerTenant();
    }
}
