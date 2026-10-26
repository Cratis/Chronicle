// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Captures;
using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.EventSources;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.ExternalServices;
using Cratis.Chronicle.Storage.Namespaces;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Chronicle.Storage.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Storage.Observation.Reactors;
using Cratis.Chronicle.Storage.Observation.Reducers;
using Cratis.Chronicle.Storage.Observation.Webhooks;
using Cratis.Chronicle.Storage.Projections;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Seeding;
using Cratis.Chronicle.Storage.SequenceQueries;

namespace Cratis.Chronicle.Storage;

/// <summary>
/// Defines the shared storage for an event store.
/// </summary>
public interface IEventStoreStorage
{
    /// <summary>
    /// Gets durable generation initialization evidence.
    /// </summary>
    Generations.IGenerationInitializationStorage GenerationInitialization { get; }

    /// <summary>Gets the durable generation compatibility floor.</summary>
    Generations.IGenerationFloorStorage GenerationFloor { get; }

    /// <summary>
    /// Gets the durable lifecycle fan-out intents for this event store.
    /// </summary>
    /// <exception cref="Occurrences.OccurrencesStorageNotSupported">The provider has not implemented lifecycle intents.</exception>
    Occurrences.ILifecycleFanOutStorage LifecycleFanOut => throw new Occurrences.OccurrencesStorageNotSupported();

    /// <summary>
    /// Gets the payload contracts registered for application and lifecycle triggers.
    /// </summary>
    /// <exception cref="Occurrences.OccurrencesStorageNotSupported">The custom provider has not implemented trigger definition storage.</exception>
    Occurrences.ITriggerDefinitionsStorage TriggerDefinitions => throw new Occurrences.OccurrencesStorageNotSupported();

    /// <summary>
    /// Gets application-scoped occurrences, separately from every namespace's queue.
    /// </summary>
    /// <exception cref="Occurrences.OccurrencesStorageNotSupported">The custom provider has not implemented occurrence storage.</exception>
    Occurrences.IOccurrencesStorage Occurrences => throw new Occurrences.OccurrencesStorageNotSupported();

    /// <summary>
    /// Gets event-store definitions and application-scoped schedule cursors.
    /// </summary>
    /// <exception cref="Occurrences.OccurrencesStorageNotSupported">The custom provider has not implemented schedule storage.</exception>
    Occurrences.IScheduleStorage Schedules => throw new Occurrences.OccurrencesStorageNotSupported();

    /// <summary>
    /// Gets the event store the storage represents.
    /// </summary>
    EventStoreName EventStore { get; }

    /// <summary>
    /// Gets the <see cref="INamespaceStorage"/> for the event store.
    /// </summary>
    INamespaceStorage Namespaces { get; }

    /// <summary>
    /// Gets the <see cref="IEventTypesStorage"/> for the event store.
    /// </summary>
    IEventTypesStorage EventTypes { get; }

    /// <summary>
    /// Gets the <see cref="IEventSourcesStorage"/> for registered event source definitions.
    /// </summary>
    IEventSourcesStorage EventSources { get; }

    /// <summary>
    /// Gets the <see cref="IUniqueConstraintsStorage"/> for the event store.
    /// </summary>
    IConstraintsStorage Constraints { get; }

    /// <summary>
    /// Gets the event-store-level unique-constraint encoding key storage, shared by all namespaces.
    /// </summary>
    /// <exception cref="UniqueConstraintEncodingStorageNotSupported">The provider has not implemented encoding-key storage.</exception>
    IUniqueConstraintEncodingStorage UniqueConstraintEncoding => throw new UniqueConstraintEncodingStorageNotSupported();

    /// <summary>
    /// Gets the <see cref="IObserverDefinitionsStorage"/> for the event store.
    /// </summary>
    IObserverDefinitionsStorage Observers { get; }

    /// <summary>
    /// Gets the <see cref="IReactorDefinitionsStorage"/> for the event store.
    /// </summary>
    IReactorDefinitionsStorage Reactors { get; }

    /// <summary>
    /// Gets the <see cref="IReducerDefinitionsStorage"/> for the event store.
    /// </summary>
    IReducerDefinitionsStorage Reducers { get; }

    /// <summary>
    /// Gets the <see cref="IProjectionDefinitionsStorage"/> for the event store.
    /// </summary>
    IProjectionDefinitionsStorage Projections { get; }

    /// <summary>
    /// Gets the <see cref="IWebhookDefinitionsStorage"/> for the event store.
    /// </summary>
    IWebhookDefinitionsStorage Webhooks { get; }

    /// <summary>
    /// Gets the <see cref="IExternalServiceDefinitionsStorage"/> for the event store.
    /// </summary>
    IExternalServiceDefinitionsStorage ExternalServices { get; }

    /// <summary>
    /// Gets the <see cref="ISequenceQueryStorage"/> for the event store.
    /// </summary>
    ISequenceQueryStorage SequenceQueries { get; }

    /// <summary>
    /// Gets the <see cref="ICapturesStorage"/> for the event store.
    /// </summary>
    ICapturesStorage Captures { get; }

    /// <summary>
    /// Gets the <see cref="IEventStoreSubscriptionDefinitionsStorage"/> for the event store.
    /// </summary>
    IEventStoreSubscriptionDefinitionsStorage EventStoreSubscriptions { get; }

    /// <summary>
    /// Gets the <see cref="IReadModelDefinitionsStorage"/> for the event store.
    /// </summary>
    IReadModelDefinitionsStorage ReadModels { get; }

    /// <summary>
    /// Gets the <see cref="IEventSeedingStorage"/> for global event seeding at the event store level.
    /// </summary>
    IEventSeedingStorage EventSeeding { get; }

    /// <summary>
    /// Checks whether first-time kernel setup completed. Missing markers in older stores mean uninitialized.
    /// </summary>
    /// <returns>Whether initialization completed.</returns>
    /// <exception cref="EventStoreInitializationNotSupported">The provider does not support initialization markers.</exception>
    Task<bool> IsInitialized() => throw new EventStoreInitializationNotSupported();

    /// <summary>
    /// Durably records successful first-time kernel setup. Only the store's initializer grain writes this marker.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="EventStoreInitializationNotSupported">The provider does not support initialization markers.</exception>
    Task MarkInitialized() => throw new EventStoreInitializationNotSupported();

    /// <summary>
    /// Checks the observer-enablement marker, independently of first-time kernel setup.
    /// Missing markers in legacy stores mean observer initialization has not completed.
    /// </summary>
    /// <returns>Whether observer initialization completed for the event store.</returns>
    /// <exception cref="EventStoreInitializationNotSupported">The provider does not support initialization markers.</exception>
    Task<bool> AreObserversInitialized() => throw new EventStoreInitializationNotSupported();

    /// <summary>
    /// Persists observer enablement after the initialization operation's cluster checks.
    /// This storage operation performs no initialization or runtime state changes itself.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="EventStoreInitializationNotSupported">The provider does not support initialization markers.</exception>
    Task MarkObserversInitialized() => throw new EventStoreInitializationNotSupported();

    /// <summary>
    /// Get a specific <see cref="IEventStoreNamespaceStorage"/> for a <see cref="EventStoreNamespaceName"/>.
    /// </summary>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/> to get for.</param>
    /// <returns>The <see cref="IEventStoreNamespaceStorage"/> instance.</returns>
    /// <remarks>
    /// Every caller asking for the same <see cref="EventStoreNamespaceName"/> gets the same instance, including
    /// callers racing on a cold cache. The namespace storage owns the namespace's sinks, and those keep in-memory
    /// state — bulk-mode flags, in-replay flags, per-key state caches — that a second instance would not see. Two
    /// instances for one namespace split that state, which is the failure <see cref="IStorage.Clear"/> describes.
    /// Namespace names are matched exactly; they are derived byte for byte into physical database names.
    /// </remarks>
    IEventStoreNamespaceStorage GetNamespace(EventStoreNamespaceName @namespace);

    /// <summary>
    /// Drop the data stored for a namespace. Called once the namespace's observers have been removed.
    /// </summary>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/> whose data to drop.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// Storage providers that keep a namespace's data apart from the other namespaces override this. The default
    /// does nothing, leaving the data in place.
    /// </remarks>
    Task DropNamespace(EventStoreNamespaceName @namespace) => Task.CompletedTask;
}
