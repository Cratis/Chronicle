// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.EventSequences;
using Cratis.Kernel.Storage;
using Cratis.Kernel.Storage.Sinks;
using Cratis.Models;
using Cratis.Sinks;

namespace Cratis.Kernel.Grains.Sinks.Outbox;

/// <summary>
/// Represents a <see cref="ISinkFactory"/> for creating <see cref="OutboxSink"/> instances.
/// </summary>
public class OutboxSinkFactory : ISinkFactory
{
    readonly IStorage _storage;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    readonly IGrainFactory _grainFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxSinkFactory"/> class.
    /// </summary>
    /// <param name="storage"><see cref="IStorage"/> for accessing the underlying storage.</param>
    /// <param name="jsonSerializerOptions">The global serialization options.</param>
    /// <param name="grainFactory"><see cref="IGrainFactory"/> for getting grains.</param>
    public OutboxSinkFactory(
        IStorage storage,
        JsonSerializerOptions jsonSerializerOptions,
        IGrainFactory grainFactory)
    {
        _storage = storage;
        _jsonSerializerOptions = jsonSerializerOptions;
        _grainFactory = grainFactory;
    }

    /// <inheritdoc/>
    public SinkTypeId TypeId => WellKnownSinkTypes.Outbox;

    /// <inheritdoc/>
    public ISink CreateFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, Model model) =>
        new OutboxSink(
            eventStore,
            @namespace,
            model,
            _storage.GetEventStore(eventStore).GetNamespace(@namespace).GetEventSequence(EventSequenceId.Outbox),
            _jsonSerializerOptions,
            _grainFactory);
}
