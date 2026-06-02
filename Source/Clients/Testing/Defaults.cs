// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Testing.Events;

namespace Cratis.Chronicle.Testing;

/// <summary>
/// Represents default implementations for Chronicle services.
/// </summary>
public class Defaults
{
    /// <summary>
    /// Get the singleton instance.
    /// </summary>
    public static readonly Defaults Instance = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="Defaults"/> class.
    /// </summary>
    public Defaults()
    {
        EventStore = new EventStoreForTesting();
        var testingStore = (EventStoreForTesting)EventStore;
        JsonSchemaGenerator = testingStore.JsonSchemaGenerator;
        ClientArtifactsProvider = testingStore.ClientArtifactsProvider;
        EventTypes = testingStore.EventTypes;
        EventSerializer = testingStore.EventSerializer;
    }

    /// <summary>
    /// Gets the default <see cref="IEventStore"/>.
    /// </summary>
    public IEventStore EventStore { get; }

    /// <summary>
    /// Gets the default <see cref="IEventTypes"/>.
    /// </summary>
    public IEventTypes EventTypes { get; }

    /// <summary>
    /// Gets the default <see cref="IJsonSchemaGenerator"/>.
    /// </summary>
    public IJsonSchemaGenerator JsonSchemaGenerator { get; }

    /// <summary>
    /// Gets the default <see cref="IClientArtifactsProvider"/>.
    /// </summary>
    public IClientArtifactsProvider ClientArtifactsProvider { get; }

    /// <summary>
    /// Gets the default <see cref="IEventSerializer"/>.
    /// </summary>
    public IEventSerializer EventSerializer { get; }
}
