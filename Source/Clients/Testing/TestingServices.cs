// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;

using System.Text.Json;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Compliance;
using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Contracts.Events.Constraints;
using Cratis.Chronicle.Contracts.EventSequences;
using Cratis.Chronicle.Contracts.Host;
using Cratis.Chronicle.Contracts.Identities;
using Cratis.Chronicle.Contracts.Jobs;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Contracts.Observation.Webhooks;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Contracts.Recommendations;
using Cratis.Chronicle.Contracts.Security;
using Cratis.Chronicle.Contracts.Seeding;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Types;
using KernelApplicationsService = KernelCore::Cratis.Chronicle.Services.Security.Applications;
using KernelComplianceService = KernelCore::Cratis.Chronicle.Services.Compliance.ComplianceService;
using KernelConstraintsService = KernelCore::Cratis.Chronicle.Services.Events.Constraints.Constraints;
using KernelEventSequencesService = KernelCore::Cratis.Chronicle.Services.EventSequences.EventSequences;
using KernelEventStoresService = KernelCore::Cratis.Chronicle.Services;
using KernelEventTypesService = KernelCore::Cratis.Chronicle.Services.Events.EventTypes;
using KernelFailedPartitionsService = KernelCore::Cratis.Chronicle.Services.Observation.FailedPartitions;
using KernelIdentitiesService = KernelCore::Cratis.Chronicle.Services.Identities.Identities;
using KernelJobsService = KernelCore::Cratis.Chronicle.Services.Jobs.Jobs;
using KernelJsonComplianceManager = KernelCore::Cratis.Chronicle.Compliance.JsonComplianceManager;
using KernelJsonCompliancePropertyValueHandler = KernelCore::Cratis.Chronicle.Compliance.IJsonCompliancePropertyValueHandler;
using KernelNamespacesService = KernelCore::Cratis.Chronicle.Services.Namespaces;
using KernelObserversService = KernelCore::Cratis.Chronicle.Services.Observation.Observers;
using KernelProjectionsService = KernelCore::Cratis.Chronicle.Services.Projections.Projections;
using KernelReactorMediator = KernelCore::Cratis.Chronicle.Observation.Reactors.Clients.ReactorMediator;
using KernelReactorsService = KernelCore::Cratis.Chronicle.Services.Observation.Reactors.Reactors;
using KernelReadModelsService = KernelCore::Cratis.Chronicle.Services.ReadModels.ReadModels;
using KernelRecommendationsService = KernelCore::Cratis.Chronicle.Services.Recommendations.Recommendations;
using KernelReducerMediator = KernelCore::Cratis.Chronicle.Observation.Reducers.Clients.ReducerMediator;
using KernelReducersService = KernelCore::Cratis.Chronicle.Services.Observation.Reducers.Reducers;
using KernelSeedingService = KernelCore::Cratis.Chronicle.Services.Seeding.EventSeeding;
using KernelServerService = KernelCore::Cratis.Chronicle.Services.Host.Server;
using KernelSubscriptionsService = KernelCore::Cratis.Chronicle.Services.Observation.EventStoreSubscriptions.EventStoreSubscriptions;
using KernelUsersService = KernelCore::Cratis.Chronicle.Services.Security.Users;
using KernelWebhookComparer = KernelCore::Cratis.Chronicle.Observation.Webhooks.WebhookDefinitionComparer;
using KernelWebhookMediatorImpl = KernelCore::Cratis.Chronicle.Observation.Webhooks.WebhookMediator;
using KernelWebhooksService = KernelCore::Cratis.Chronicle.Services.Observation.Webhooks.Webhooks;

namespace Cratis.Chronicle.Testing;

/// <summary>
/// Represents an implementation of <see cref="IServices"/> for testing scenarios backed by real kernel
/// gRPC service implementations wired to in-memory storage.
/// </summary>
/// <remarks>
/// All gRPC service contracts are backed by the real kernel implementations from
/// <c>Cratis.Chronicle.Services</c>.
/// </remarks>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for grain-based operations.</param>
/// <param name="storage">The <see cref="IStorage"/> backed by in-memory implementations.</param>
/// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> for serialization.</param>
internal sealed class TestingServices(
    IGrainFactory grainFactory,
    IStorage storage,
    JsonSerializerOptions jsonSerializerOptions) : IServices
{
    readonly Lazy<IObservers> _observers = new(() =>
        new KernelObserversService(grainFactory, storage));

    readonly Lazy<IFailedPartitions> _failedPartitions = new(() =>
        new KernelFailedPartitionsService(storage));

    readonly Lazy<IReactors> _reactors = new(() =>
        new KernelReactorsService(
            grainFactory,
            new KernelReactorMediator(),
            storage,
            jsonSerializerOptions,
            NullLogger<KernelReactorsService>.Instance));

    readonly Lazy<IReducers> _reducers = new(() =>
        new KernelReducersService(
            grainFactory,
            new KernelReducerMediator(),
            new ExpandoObjectConverter(new TypeFormats()),
            jsonSerializerOptions,
            NullLogger<KernelReducersService>.Instance));

    readonly Lazy<IProjections> _projections = new(() =>
        new KernelProjectionsService(
            grainFactory,
            new ExpandoObjectConverter(new TypeFormats()),
            null!,
            null!));

    readonly Lazy<IWebhooks> _webhooks = new(() =>
        new KernelWebhooksService(
            grainFactory,
            storage,
            new KernelWebhookComparer(
                storage,
                new ObjectComparer(),
                NullLogger<KernelWebhookComparer>.Instance),
            null!,
            null!,
            new KernelWebhookMediatorImpl(null!, jsonSerializerOptions)));

    readonly Lazy<IEventStoreSubscriptions> _eventStoreSubscriptions = new(() =>
        new KernelSubscriptionsService(grainFactory, storage));

    readonly Lazy<IJobs> _jobs = new(() =>
        new KernelJobsService(grainFactory, storage));

    readonly Lazy<IEventSeeding> _seeding = new(() =>
        new KernelSeedingService(grainFactory));

    readonly Lazy<IEventSequences> _eventSequences = new(() =>
        new KernelEventSequencesService(
            grainFactory,
            storage,
            new KernelJsonComplianceManager(new KnownInstancesOf<KernelJsonCompliancePropertyValueHandler>()),
            new ExpandoObjectConverter(new TypeFormats()),
            jsonSerializerOptions));

    readonly Lazy<INamespaces> _namespaces = new(() =>
        new KernelNamespacesService(grainFactory, storage));

    readonly Lazy<IIdentities> _identities = new(() =>
        new KernelIdentitiesService(storage));

    readonly Lazy<IEventTypes> _eventTypes = new(() =>
        new KernelEventTypesService(storage, grainFactory));

    readonly Lazy<IRecommendations> _recommendations = new(() =>
        new KernelRecommendationsService(grainFactory, storage));

    readonly Lazy<IConstraints> _constraints = new(() =>
        new KernelConstraintsService(grainFactory));

    readonly Lazy<IUsers> _users = new(() =>
        new KernelUsersService(grainFactory, storage));

    readonly Lazy<IApplications> _applications = new(() =>
        new KernelApplicationsService(grainFactory, storage));

    readonly Lazy<IServer> _server = new(() =>
        new KernelServerService(null!));

    readonly Lazy<IEventStores> _eventStores = new(() =>
        new KernelEventStoresService.EventStores(grainFactory, storage, null!, null!));

    readonly Lazy<IReadModels> _readModels = new(() =>
        new KernelReadModelsService(
            null!,
            grainFactory,
            storage,
            new ExpandoObjectConverter(new TypeFormats()),
            new KernelJsonComplianceManager(new KnownInstancesOf<KernelJsonCompliancePropertyValueHandler>()),
            jsonSerializerOptions));

    readonly Lazy<ICompliance> _compliance = new(() =>
        new KernelComplianceService(
            new KernelJsonComplianceManager(new KnownInstancesOf<KernelJsonCompliancePropertyValueHandler>()),
            NullLogger<KernelComplianceService>.Instance));

    /// <inheritdoc/>
    public IReadModels ReadModels => _readModels.Value;

    /// <inheritdoc/>
    public ICompliance Compliance => _compliance.Value;

    /// <inheritdoc/>
    public IConstraints Constraints => _constraints.Value;

    /// <inheritdoc/>
    public IObservers Observers => _observers.Value;

    /// <inheritdoc/>
    public IFailedPartitions FailedPartitions => _failedPartitions.Value;

    /// <inheritdoc/>
    public IReactors Reactors => _reactors.Value;

    /// <inheritdoc/>
    public IReducers Reducers => _reducers.Value;

    /// <inheritdoc/>
    public IProjections Projections => _projections.Value;

    /// <inheritdoc/>
    public IWebhooks Webhooks => _webhooks.Value;

    /// <inheritdoc/>
    public IEventStoreSubscriptions EventStoreSubscriptions => _eventStoreSubscriptions.Value;

    /// <inheritdoc/>
    public IJobs Jobs => _jobs.Value;

    /// <inheritdoc/>
    public IEventSeeding Seeding => _seeding.Value;

    /// <inheritdoc/>
    public IEventSequences EventSequences => _eventSequences.Value;

    /// <inheritdoc/>
    public IEventStores EventStores => _eventStores.Value;

    /// <inheritdoc/>
    public INamespaces Namespaces => _namespaces.Value;

    /// <inheritdoc/>
    public IIdentities Identities => _identities.Value;

    /// <inheritdoc/>
    public IEventTypes EventTypes => _eventTypes.Value;

    /// <inheritdoc/>
    public IRecommendations Recommendations => _recommendations.Value;

    /// <inheritdoc/>
    public IUsers Users => _users.Value;

    /// <inheritdoc/>
    public IApplications Applications => _applications.Value;

    /// <inheritdoc/>
    public IServer Server => _server.Value;
}
