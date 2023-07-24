// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Engines.Sinks;
using Aksio.Cratis.Projections;
using Aksio.Cratis.Sinks;

namespace Aksio.Cratis.Kernel.Grains.Sinks.Outbox;

/// <summary>
/// Represents a <see cref="IProjectionSinkFactory"/> for creating <see cref="OutboxProjectionSink"/> instances.
/// </summary>
public class OutboxProjectionSinkFactory : IProjectionSinkFactory
{
    readonly IEventSequenceStorage _eventSequenceStorageProvider;
    readonly IExecutionContextManager _executionContextManager;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    readonly IGrainFactory _grainFactory;

    /// <inheritdoc/>
    public SinkTypeId TypeId => WellKnownSinkTypes.Outbox;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxProjectionSinkFactory"/> class.
    /// </summary>
    /// <param name="eventSequenceStorageProvider">The <see cref="IEventSequenceStorage"/>.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="jsonSerializerOptions">The global serialization options.</param>
    /// <param name="grainFactory"><see cref="IGrainFactory"/> for getting grains.</param>
    public OutboxProjectionSinkFactory(
        IEventSequenceStorage eventSequenceStorageProvider,
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
    public ISink CreateFor(Model model) =>
        new OutboxProjectionSink(
            model,
            _eventSequenceStorageProvider,
            _executionContextManager,
            _jsonSerializerOptions,
            _grainFactory);
}
