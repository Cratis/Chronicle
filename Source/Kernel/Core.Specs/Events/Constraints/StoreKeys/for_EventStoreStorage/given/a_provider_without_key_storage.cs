// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Events.Constraints.StoreKeys.for_EventStoreStorage.given;

/// <summary>
/// Represents a custom provider that relies on the default unsupported encoding-storage capability.
/// </summary>
public sealed class a_provider_without_key_storage : IEventStoreStorage
{
    /// <inheritdoc/>
    public Storage.Generations.IGenerationInitializationStorage GenerationInitialization => throw new NotSupportedException();

    /// <inheritdoc/>
    public Storage.Generations.IGenerationFloorStorage GenerationFloor => throw new NotSupportedException();

    /// <inheritdoc/>
    public EventStoreName EventStore => throw new NotSupportedException();

    /// <inheritdoc/>
    public Storage.Namespaces.INamespaceStorage Namespaces => throw new NotSupportedException();

    /// <inheritdoc/>
    public Storage.EventTypes.IEventTypesStorage EventTypes => throw new NotSupportedException();

    /// <inheritdoc/>
    public Storage.EventSources.IEventSourcesStorage EventSources => throw new NotSupportedException();

    /// <inheritdoc/>
    public Storage.Events.Constraints.IConstraintsStorage Constraints => throw new NotSupportedException();

    /// <inheritdoc/>
    public Storage.Observation.IObserverDefinitionsStorage Observers => throw new NotSupportedException();

    /// <inheritdoc/>
    public Storage.Observation.Reactors.IReactorDefinitionsStorage Reactors => throw new NotSupportedException();

    /// <inheritdoc/>
    public Storage.Observation.Reducers.IReducerDefinitionsStorage Reducers => throw new NotSupportedException();

    /// <inheritdoc/>
    public Storage.Projections.IProjectionDefinitionsStorage Projections => throw new NotSupportedException();

    /// <inheritdoc/>
    public Storage.Observation.Webhooks.IWebhookDefinitionsStorage Webhooks => throw new NotSupportedException();

    /// <inheritdoc/>
    public Storage.ExternalServices.IExternalServiceDefinitionsStorage ExternalServices => throw new NotSupportedException();

    /// <inheritdoc/>
    public Storage.SequenceQueries.ISequenceQueryStorage SequenceQueries => throw new NotSupportedException();

    /// <inheritdoc/>
    public Storage.Captures.ICapturesStorage Captures => throw new NotSupportedException();

    /// <inheritdoc/>
    public Storage.Observation.EventStoreSubscriptions.IEventStoreSubscriptionDefinitionsStorage EventStoreSubscriptions => throw new NotSupportedException();

    /// <inheritdoc/>
    public Storage.ReadModels.IReadModelDefinitionsStorage ReadModels => throw new NotSupportedException();

    /// <inheritdoc/>
    public Storage.Seeding.IEventSeedingStorage EventSeeding => throw new NotSupportedException();

    /// <inheritdoc/>
    public IEventStoreNamespaceStorage GetNamespace(EventStoreNamespaceName @namespace) => throw new NotSupportedException();
}
