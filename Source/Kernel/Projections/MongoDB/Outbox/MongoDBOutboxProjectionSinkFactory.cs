// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Execution;
using Orleans;

namespace Aksio.Cratis.Events.Projections.MongoDB;

/// <summary>
/// Represents a <see cref="IProjectionSinkFactory"/> for creating <see cref="MongoDBOutboxProjectionSink"/> instances.
/// </summary>
public class MongoDBOutboxProjectionSinkFactory : IProjectionSinkFactory
{
    readonly IEventSequenceStorageProvider _eventSequenceStorageProvider;
    readonly IExecutionContextManager _executionContextManager;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    readonly IGrainFactory _grainFactory;

    /// <inheritdoc/>
    public ProjectionSinkTypeId TypeId => WellKnownProjectionSinkTypes.Outbox;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBOutboxProjectionSinkFactory"/> class.
    /// </summary>
    /// <param name="eventSequenceStorageProvider">The <see cref="IEventSequenceStorageProvider"/>.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="jsonSerializerOptions">The global serialization options.</param>
    /// <param name="grainFactory"><see cref="IGrainFactory"/> for getting grains.</param>
    public MongoDBOutboxProjectionSinkFactory(
        IEventSequenceStorageProvider eventSequenceStorageProvider,
        IExecutionContextManager executionContextManager,
        JsonSerializerOptions jsonSerializerOptions,
        IGrainFactory grainFactory)
    {
        _eventSequenceStorageProvider = eventSequenceStorageProvider;
        _executionContextManager = executionContextManager;
        _jsonSerializerOptions = jsonSerializerOptions;
        _grainFactory = grainFactory;
    }

    /// <inheritdoc/>
    public IProjectionSink CreateFor(Model model) =>
        new MongoDBOutboxProjectionSink(
            model,
            _eventSequenceStorageProvider,
            _executionContextManager,
            _jsonSerializerOptions,
            _grainFactory);
}
