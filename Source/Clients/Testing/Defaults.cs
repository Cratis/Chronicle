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
        : this(DefaultClientArtifactsProvider.Default)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Defaults"/> class with per-run artifact discovery.
    /// </summary>
    /// <param name="clientArtifactsProvider">The artifacts available to this run.</param>
    /// <param name="serviceProvider">Optional service provider for activating artifacts.</param>
    public Defaults(IClientArtifactsProvider clientArtifactsProvider, IServiceProvider? serviceProvider = null)
        : this(clientArtifactsProvider, serviceProvider, null)
    {
    }

    /// <summary>
    /// Initializes per-run services with Chronicle settings, including explicit artifacts.
    /// </summary>
    /// <param name="clientArtifactsProvider">The artifacts available to this run.</param>
    /// <param name="serviceProvider">Optional service provider for activating artifacts.</param>
    /// <param name="options">Optional Chronicle settings for this run.</param>
    public Defaults(IClientArtifactsProvider clientArtifactsProvider, IServiceProvider? serviceProvider, ChronicleOptions? options)
    {
        EventStore = new EventStoreForTesting(serviceProvider, clientArtifactsProvider, options);
        var testingStore = (EventStoreForTesting)EventStore;
        JsonSchemaGenerator = testingStore.JsonSchemaGenerator;
        ClientArtifactsProvider = testingStore.ClientArtifactsProvider;
        Options = testingStore.ClientOptions;
        ScenarioOptions = options;
        EventTypes = testingStore.EventTypes;
        EventSerializer = testingStore.EventSerializer;
    }

    /// <summary>
    /// Gets the Chronicle settings used by this run.
    /// </summary>
    public ChronicleOptions Options { get; }

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

    /// <summary>
    /// Gets the settings explicitly supplied to this run, or <see langword="null"/> so each scenario resolves its own settings as before.
    /// </summary>
    internal ChronicleOptions? ScenarioOptions { get; }
}
