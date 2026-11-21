// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Captures;
using Cratis.Chronicle.Contracts.Compliance;
using Cratis.Chronicle.Contracts.Events.Constraints;
using Cratis.Chronicle.Contracts.EventSources;
using Cratis.Chronicle.Contracts.EventStores;
using Cratis.Chronicle.Contracts.EventTypes;
using Cratis.Chronicle.Contracts.ExternalServices;
using Cratis.Chronicle.Contracts.Host;
using Cratis.Chronicle.Contracts.Identities;
using Cratis.Chronicle.Contracts.Jobs;
using Cratis.Chronicle.Contracts.Namespaces;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Contracts.Observation.Webhooks;
using Cratis.Chronicle.Contracts.Patterns;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Contracts.Recommendations;
using Cratis.Chronicle.Contracts.Security;
using Cratis.Chronicle.Contracts.Seeding;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Testing.Compliance;

namespace Cratis.Chronicle.Testing;

/// <summary>
/// Represents an implementation of <see cref="IServices"/> for testing scenarios backed by real kernel
/// gRPC service implementations wired to in-memory storage.
/// </summary>
/// <remarks>
/// All gRPC service contracts are backed by the real kernel implementations from
/// <c language="csharp">Cratis.Chronicle.Services</c>. Command-dispatching implementations execute through one shared Arc
/// command pipeline, whose service provider carries the in-memory collaborators the command handlers
/// resolve their parameters from.
/// </remarks>
internal sealed partial class TestingServices : IServices
{
    readonly Lazy<IObservers> _observers;
    readonly Lazy<IFailedPartitions> _failedPartitions;
    readonly Lazy<IReactors> _reactors;
    readonly Lazy<IReducers> _reducers;
    readonly Lazy<IProjections> _projections;
    readonly Lazy<IWebhooks> _webhooks;
    readonly Lazy<IExternalServices> _externalServices;
    readonly Lazy<ICaptures> _captures;
    readonly Lazy<Contracts.Occurrences.IOccurrences> _occurrences;
    readonly Lazy<IEventStoreSubscriptions> _eventStoreSubscriptions;
    readonly Lazy<IJobs> _jobs;
    readonly Lazy<IEventSeeding> _seeding;
    readonly Lazy<Contracts.Sequences.IEventSequences> _sequences;
    readonly Lazy<INamespaces> _namespaces;
    readonly Lazy<IIdentities> _identities;
    readonly Lazy<IEventTypes> _eventTypes;
    readonly Lazy<IEventSources> _eventSources;
    readonly Lazy<IPatterns> _patterns;
    readonly Lazy<IRecommendations> _recommendations;
    readonly Lazy<IConstraints> _constraints;
    readonly Lazy<IUsers> _users;
    readonly Lazy<IApplications> _applications;
    readonly Lazy<IServer> _server;
    readonly Lazy<IEventStores> _eventStores;
    readonly Lazy<IReadModels> _readModels;
    readonly Lazy<ICompliance> _compliance;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestingServices"/> class.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> for grain-based operations.</param>
    /// <param name="storage">The <see cref="IStorage"/> backed by in-memory implementations.</param>
    /// <param name="compliance">The scenario's shared compliance stack and encryption keys.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> for serialization.</param>
    /// <param name="eventTypes">Resolves the event types belonging to this test store.</param>
    /// <param name="replies">Optional reply-loss driver.</param>
    public TestingServices(
        IGrainFactory grainFactory,
        IStorage storage,
        InProcessCompliance compliance,
        JsonSerializerOptions jsonSerializerOptions,
        Func<Cratis.Chronicle.Events.IEventTypes> eventTypes,
        DropReplyInterceptor? replies = null)
    {
        var context = new TestingServiceContext
        {
            GrainFactory = grainFactory,
            Storage = storage,
            Compliance = compliance,
            JsonSerializerOptions = jsonSerializerOptions,
            EventTypes = eventTypes,
            Replies = replies
        }.Initialize();

        _observers = new(() => CreateObservers(context));
        _failedPartitions = new(() => CreateFailedPartitions(context));
        _reactors = new(() => CreateReactors(context));
        _reducers = new(() => CreateReducers(context));
        _projections = new(() => CreateProjections(context));
        _webhooks = new(() => CreateWebhooks(context));
        _eventStoreSubscriptions = new(() => CreateEventStoreSubscriptions(context));
        _externalServices = new(() => CreateExternalServices(context));
        _captures = new(() => CreateCaptures(context));
        _occurrences = new(() => CreateOccurrences(context));
        _jobs = new(() => CreateJobs(context));
        _seeding = new(() => CreateSeeding(context));
        _sequences = new(() => CreateSequences(context));
        _namespaces = new(() => CreateNamespaces(context));
        _identities = new(() => CreateIdentities(context));
        _eventSources = new(() => CreateEventSources(context));
        _eventTypes = new(() => CreateEventTypes(context));
        _patterns = new(() => CreatePatterns(context));
        _recommendations = new(() => CreateRecommendations(context));
        _constraints = new(() => CreateConstraints(context));
        _users = new(() => CreateUsers(context));
        _applications = new(() => CreateApplications(context));
        _server = new(CreateServer);
        _eventStores = new(() => CreateEventStores(context));
        _readModels = new(() => CreateReadModels(context));
        _compliance = new(() => CreateCompliance(context));
    }

    /// <inheritdoc/>
    public IReadModels ReadModels => _readModels.Value;

    /// <inheritdoc/>
    public IMaterializedReadModels MaterializedReadModels => throw new NotSupportedException("MaterializedReadModels is not supported in test scenarios.");

    /// <inheritdoc/>
    public Contracts.ReadModelExplorer.IReadModelExplorer ReadModelExplorer => throw new NotSupportedException("ReadModelExplorer is not supported in test scenarios.");

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
    public IExternalServices ExternalServices => _externalServices.Value;

    /// <inheritdoc/>
    public ICaptures Captures => _captures.Value;

    /// <inheritdoc/>
    public Contracts.Occurrences.IOccurrences Occurrences => _occurrences.Value;

    /// <inheritdoc/>
    public IEventStoreSubscriptions EventStoreSubscriptions => _eventStoreSubscriptions.Value;

    /// <inheritdoc/>
    public IJobs Jobs => _jobs.Value;

    /// <inheritdoc/>
    public IEventSeeding Seeding => _seeding.Value;

    /// <inheritdoc/>
    public Contracts.Sequences.IEventSequences Sequences => _sequences.Value;

    /// <inheritdoc/>
    public IEventStores EventStores => _eventStores.Value;

    /// <inheritdoc/>
    public INamespaces Namespaces => _namespaces.Value;

    /// <inheritdoc/>
    public IIdentities Identities => _identities.Value;

    /// <inheritdoc/>
    public IEventTypes EventTypes => _eventTypes.Value;

    /// <inheritdoc/>
    public IEventSources EventSources => _eventSources.Value;

    /// <inheritdoc/>
    public IPatterns Patterns => _patterns.Value;

    /// <inheritdoc/>
    public IRecommendations Recommendations => _recommendations.Value;

    /// <inheritdoc/>
    public IUsers Users => _users.Value;

    /// <inheritdoc/>
    public IApplications Applications => _applications.Value;

    /// <inheritdoc/>
    public IServer Server => _server.Value;

    /// <inheritdoc/>
    public Contracts.Clients.IConnectionService Connections => throw new NotSupportedException("Connections is not supported in test scenarios.");
}
