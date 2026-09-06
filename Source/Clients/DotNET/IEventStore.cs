// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventStoreSubscriptions;
using Cratis.Chronicle.ExternalServices;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Patterns;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Registrations;
using Cratis.Chronicle.Seeding;
using Cratis.Chronicle.Transactions;
using Cratis.Chronicle.Webhooks;

namespace Cratis.Chronicle;

/// <summary>
/// Defines the event store API surface.
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// Gets the <see cref="Name"/> for the event store.
    /// </summary>
    EventStoreName Name { get; }

    /// <summary>
    /// Gets the namespace for the event store.
    /// </summary>
    EventStoreNamespaceName Namespace { get; }

    /// <summary>
    /// Gets the <see cref="IChronicleConnection"/> used for the <see cref="IEventStore"/>.
    /// </summary>
    IChronicleConnection Connection { get; }

    /// <summary>
    /// Gets the <see cref="IClientArtifactsProvider"/> for the event store.
    /// </summary>
    IUnitOfWorkManager UnitOfWorkManager { get; }

    /// <summary>
    /// Gets the <see cref="IEventTypes"/> for the event store.
    /// </summary>
    IEventTypes EventTypes { get; }

    /// <summary>
    /// Gets the <see cref="IConstraints"/> for the event store.
    /// </summary>
    IConstraints Constraints { get; }

    /// <summary>
    /// Gets the <see cref="IEventLog"/> event sequence.
    /// </summary>
    IEventLog EventLog { get; }

    /// <summary>
    /// Gets the <see cref="IJobs"/> for the event store.
    /// </summary>
    IJobs Jobs { get; }

    /// <summary>
    /// Gets the <see cref="IReactors"/> for the event store.
    /// </summary>
    IReactors Reactors { get; }

    /// <summary>
    /// Gets the <see cref="IReducers"/> for the event store.
    /// </summary>
    IReducers Reducers { get; }

    /// <summary>
    /// Gets the <see cref="IProjections"/> for the event store.
    /// </summary>
    IProjections Projections { get; }

    /// <summary>
    /// Gets the <see cref="IWebhooks"/> for the event store.
    /// </summary>
    IWebhooks Webhooks { get; }

    /// <summary>
    /// Gets the <see cref="IExternalServices"/> for the event store.
    /// </summary>
    IExternalServices ExternalServices { get; }

    /// <summary>
    /// Gets the <see cref="IEventStoreSubscriptions"/> for the event store.
    /// </summary>
    IEventStoreSubscriptions Subscriptions { get; }

    /// <summary>
    /// Gets the <see cref="IFailedPartitions"/> for the event store.
    /// </summary>
    IFailedPartitions FailedPartitions { get; }

    /// <summary>
    /// Gets the <see cref="IReadModels"/> for the event store.
    /// </summary>
    IReadModels ReadModels { get; }

    /// <summary>
    /// Gets the <see cref="IReadModelReactors"/> for the event store.
    /// </summary>
    IReadModelReactors ReadModelReactors { get; }

    /// <summary>
    /// Gets the <see cref="IEventSeeding"/> for the event store.
    /// </summary>
    IEventSeeding Seeding { get; }

    /// <summary>
    /// Gets the <see cref="IPatterns"/> for the event store.
    /// </summary>
    /// <remarks>
    /// Optional, so that an existing implementation of this interface - a test double, a scenario harness in a
    /// consuming framework - keeps compiling and loading without change. Adding a required member to an interface
    /// this widely implemented breaks every implementer at type-load time, before any of their code runs. The real
    /// event store implements it; anything that does not says so by throwing rather than by quietly answering.
    /// </remarks>
    /// <exception cref="PatternsNotSupported">Thrown when the implementation does not support behavior patterns.</exception>
    IPatterns Patterns => throw new PatternsNotSupported(GetType());

    /// <summary>
    /// Gets the <see cref="IPIIManager"/> for managing PII encryption keys (GDPR right-to-erasure) for the event store.
    /// </summary>
    IPIIManager PII { get; }

    /// <summary>
    /// Gets the <see cref="IIdentityManager"/> for managing identities for the event store.
    /// </summary>
    IIdentityManager Identities { get; }

    /// <summary>
    /// Gets what became of the declared artifacts the last time <see cref="RegisterAll"/> ran.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is <see cref="RegistrationOutcome.NotRun"/> until <see cref="RegisterAll"/> has completed, and from then on
    /// reports every declared projection artifact as either registered or failed, carrying the failure that stopped it.
    /// It is read-only and decides nothing - what a consumer does about a failed artifact is the consumer's call.
    /// </para>
    /// <para>
    /// Registration is wired to the connection lifecycle, so it runs on its own; reading this property never triggers
    /// it. To block until it has run, use <c>RegistrationWaitExtensions.WaitForRegistration</c>. Do not poll
    /// <see cref="Connections.IConnectionLifecycle.IsConnected"/> as a substitute - see
    /// <see cref="RegistrationOutcome"/> for why that races.
    /// </para>
    /// </remarks>
    RegistrationOutcome Registration { get; }

    /// <summary>
    /// Discover all artifacts for the event store.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task DiscoverAll();

    /// <summary>
    /// Register all artifacts for the event store.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task RegisterAll();

    /// <summary>
    /// Get an event sequence by id.
    /// </summary>
    /// <param name="id">The identifier of the event sequence to get.</param>
    /// <returns><see cref="IEventSequence"/> instance.</returns>
    IEventSequence GetEventSequence(EventSequenceId id);

    /// <summary>
    /// List namespaces in the event store.
    /// </summary>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns>An asynchronous enumerable for all namespace names.</returns>
    Task<IEnumerable<EventStoreNamespaceName>> GetNamespaces(CancellationToken cancellationToken = default);
}
