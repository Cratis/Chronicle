// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Applications.Autofac;
using Aksio.Cratis.Compliance.MongoDB;
using Aksio.Cratis.Events.MongoDB.EventTypes;
using Aksio.Cratis.Kernel.Storage.Changes;
using Aksio.Cratis.Kernel.Storage.Compliance;
using Aksio.Cratis.Kernel.Storage.EventSequences;
using Aksio.Cratis.Kernel.Storage.EventTypes;
using Aksio.Cratis.Kernel.Storage.Identities;
using Aksio.Cratis.Kernel.Storage.Jobs;
using Aksio.Cratis.Kernel.Storage.Keys;
using Aksio.Cratis.Kernel.Storage.MongoDB.EventSequences;
using Aksio.Cratis.Kernel.Storage.MongoDB.Identities;
using Aksio.Cratis.Kernel.Storage.MongoDB.Jobs;
using Aksio.Cratis.Kernel.Storage.MongoDB.Keys;
using Aksio.Cratis.Kernel.Storage.MongoDB.Observation;
using Aksio.Cratis.Kernel.Storage.MongoDB.Projections;
using Aksio.Cratis.Kernel.Storage.MongoDB.Recommendations;
using Aksio.Cratis.Kernel.Storage.Observation;
using Aksio.Cratis.Kernel.Storage.Projections;
using Aksio.Cratis.Kernel.Storage.Recommendations;
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
        builder.RegisterType<EncryptionKeyStorage>().AsSelf().InstancePerMicroservice();
        builder.Register(_ => new CacheEncryptionKeyStorage(_.Resolve<EncryptionKeyStorage>())).As<IEncryptionKeyStorage>().InstancePerMicroservice();
        builder.RegisterType<EventTypesStorage>().As<IEventTypesStorage>().InstancePerMicroservice();
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

        builder.RegisterType<IdentityStorage>().As<IIdentityStorage>().InstancePerTenant();
    }
}
