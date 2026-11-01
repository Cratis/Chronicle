// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Captures;
using Cratis.Chronicle.Contracts.Clients;
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
using Grpc.Core;
using ProtoBuf.Grpc.Client;
using ProtoBuf.Grpc.Configuration;

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Represents services whose proxies are created on first use.
/// </summary>
/// <param name="callInvoker">The call invoker for this connection.</param>
/// <param name="clientFactory">The factory used to create service proxies.</param>
/// <remarks>
/// Each container belongs to one connection. Its proxies remain bound to the call invoker of that connection,
/// including after a reconnect. Concurrent first accesses may create stateless duplicates, but publish only one
/// instance. Failed creations are not cached and can be retried on the next access.
/// </remarks>
internal sealed class LazyServices(CallInvoker callInvoker, ClientFactory clientFactory) : IServices
{
    ICompliance? _compliance;
    IEventStores? _eventStores;
    INamespaces? _namespaces;
    IRecommendations? _recommendations;
    IPatterns? _patterns;
    IIdentities? _identities;
    Contracts.Sequences.IEventSequences? _sequences;
    IEventTypes? _eventTypes;
    IConstraints? _constraints;
    IObservers? _observers;
    IFailedPartitions? _failedPartitions;
    IReactors? _reactors;
    IReducers? _reducers;
    IProjections? _projections;
    IWebhooks? _webhooks;
    IExternalServices? _externalServices;
    ICaptures? _captures;
    IEventStoreSubscriptions? _eventStoreSubscriptions;
    IReadModels? _readModels;
    IMaterializedReadModels? _materializedReadModels;
    Contracts.ReadModelExplorer.IReadModelExplorer? _readModelExplorer;
    IJobs? _jobs;
    IEventSeeding? _seeding;
    IUsers? _users;
    IApplications? _applications;
    IServer? _server;
    IConnectionService? _connections;
    IEventSources? _eventSources;
    Contracts.Occurrences.IOccurrences? _occurrences;
    IAdministrators? _administrators;

    /// <inheritdoc/>
    public ICompliance Compliance => Volatile.Read(ref _compliance) ?? Create(ref _compliance);

    /// <inheritdoc/>
    public IEventStores EventStores => Volatile.Read(ref _eventStores) ?? Create(ref _eventStores);

    /// <inheritdoc/>
    public INamespaces Namespaces => Volatile.Read(ref _namespaces) ?? Create(ref _namespaces);

    /// <inheritdoc/>
    public IRecommendations Recommendations => Volatile.Read(ref _recommendations) ?? Create(ref _recommendations);

    /// <inheritdoc/>
    public IPatterns Patterns => Volatile.Read(ref _patterns) ?? Create(ref _patterns);

    /// <inheritdoc/>
    public IIdentities Identities => Volatile.Read(ref _identities) ?? Create(ref _identities);

    /// <inheritdoc/>
    public Contracts.Sequences.IEventSequences Sequences => Volatile.Read(ref _sequences) ?? Create(ref _sequences);

    /// <inheritdoc/>
    public IEventTypes EventTypes => Volatile.Read(ref _eventTypes) ?? Create(ref _eventTypes);

    /// <inheritdoc/>
    public IConstraints Constraints => Volatile.Read(ref _constraints) ?? Create(ref _constraints);

    /// <inheritdoc/>
    public IObservers Observers => Volatile.Read(ref _observers) ?? Create(ref _observers);

    /// <inheritdoc/>
    public IFailedPartitions FailedPartitions => Volatile.Read(ref _failedPartitions) ?? Create(ref _failedPartitions);

    /// <inheritdoc/>
    public IReactors Reactors => Volatile.Read(ref _reactors) ?? Create(ref _reactors);

    /// <inheritdoc/>
    public IReducers Reducers => Volatile.Read(ref _reducers) ?? Create(ref _reducers);

    /// <inheritdoc/>
    public IProjections Projections => Volatile.Read(ref _projections) ?? Create(ref _projections);

    /// <inheritdoc/>
    public IWebhooks Webhooks => Volatile.Read(ref _webhooks) ?? Create(ref _webhooks);

    /// <inheritdoc/>
    public IExternalServices ExternalServices => Volatile.Read(ref _externalServices) ?? Create(ref _externalServices);

    /// <inheritdoc/>
    public ICaptures Captures => Volatile.Read(ref _captures) ?? Create(ref _captures);

    /// <inheritdoc/>
    public IEventStoreSubscriptions EventStoreSubscriptions => Volatile.Read(ref _eventStoreSubscriptions) ?? Create(ref _eventStoreSubscriptions);

    /// <inheritdoc/>
    public IReadModels ReadModels => Volatile.Read(ref _readModels) ?? Create(ref _readModels);

    /// <inheritdoc/>
    public IMaterializedReadModels MaterializedReadModels => Volatile.Read(ref _materializedReadModels) ?? Create(ref _materializedReadModels);

    /// <inheritdoc/>
    public Contracts.ReadModelExplorer.IReadModelExplorer ReadModelExplorer => Volatile.Read(ref _readModelExplorer) ?? Create(ref _readModelExplorer);

    /// <inheritdoc/>
    public IJobs Jobs => Volatile.Read(ref _jobs) ?? Create(ref _jobs);

    /// <inheritdoc/>
    public IEventSeeding Seeding => Volatile.Read(ref _seeding) ?? Create(ref _seeding);

    /// <inheritdoc/>
    public IUsers Users => Volatile.Read(ref _users) ?? Create(ref _users);

    /// <inheritdoc/>
    public IApplications Applications => Volatile.Read(ref _applications) ?? Create(ref _applications);

    /// <inheritdoc/>
    public IServer Server => Volatile.Read(ref _server) ?? Create(ref _server);

    /// <inheritdoc/>
    public IConnectionService Connections => Volatile.Read(ref _connections) ?? Create(ref _connections);

    /// <inheritdoc/>
    public IEventSources EventSources => Volatile.Read(ref _eventSources) ?? Create(ref _eventSources);

    /// <inheritdoc/>
    public Contracts.Occurrences.IOccurrences Occurrences => Volatile.Read(ref _occurrences) ?? Create(ref _occurrences);

    /// <inheritdoc/>
    public IAdministrators Administrators => Volatile.Read(ref _administrators) ?? Create(ref _administrators);

    T Create<T>(ref T? field)
        where T : class
    {
        var created = callInvoker.CreateGrpcService<T>(clientFactory);
        return Interlocked.CompareExchange(ref field, created, null) ?? created;
    }
}
