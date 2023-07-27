// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Aksio.Cratis.Changes;
using Aksio.Cratis.Dynamic;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Engines.Keys;
using Aksio.Cratis.Kernel.Engines.Sinks;
using Aksio.Cratis.Kernel.EventSequences;
using Aksio.Cratis.Kernel.Grains.EventSequences;
using Aksio.Cratis.Objects;
using Aksio.Cratis.Projections;
using Aksio.Cratis.Schemas;
using Aksio.Cratis.Sinks;

namespace Aksio.Cratis.Kernel.Grains.Sinks.Outbox;

/// <summary>
/// Represents an implementation of <see cref="ISink"/> for projecting to the event outbox, MongoDB based.
/// </summary>
public class OutboxSink : ISink, IDisposable
{
    readonly Model _model;
    readonly IEventSequenceStorage _eventSequenceStorageProvider;
    readonly IExecutionContextManager _executionContextManager;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    readonly IGrainFactory _grainFactory;
    bool _replaying;

    /// <inheritdoc/>
    public SinkTypeId TypeId => WellKnownSinkTypes.Outbox;

    /// <inheritdoc/>
    public SinkTypeName Name => "MongoDB Outbox";

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxSink"/> class.
    /// </summary>
    /// <param name="model"><see cref="Model"/> the sink is for.</param>
    /// <param name="eventSequenceStorageProvider">The <see cref="IEventSequenceStorage"/>.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="jsonSerializerOptions">The global serialization options.</param>
    /// <param name="grainFactory"><see cref="IGrainFactory"/> for getting grains.</param>
    public OutboxSink(
        Model model,
        IEventSequenceStorage eventSequenceStorageProvider,
        IExecutionContextManager executionContextManager,
        JsonSerializerOptions jsonSerializerOptions,
        IGrainFactory grainFactory)
    {
        _model = model;
        _eventSequenceStorageProvider = eventSequenceStorageProvider;
        _executionContextManager = executionContextManager;
        _jsonSerializerOptions = jsonSerializerOptions;
        _grainFactory = grainFactory;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
    }

    /// <inheritdoc/>
    public async Task ApplyChanges(Key key, IChangeset<AppendedEvent, ExpandoObject> changeset, bool isReplaying)
    {
        var state = changeset.InitialState.Clone();
        foreach (var change in changeset.Changes)
        {
            state = state.MergeWith((change.State as ExpandoObject)!);
        }

        var eventType = _model.Schema.GetEventType();
        var outbox = _grainFactory.GetGrain<IEventSequence>(
            EventSequenceId.Outbox,
            keyExtension: _executionContextManager.Current.ToMicroserviceAndTenant());

        var stateAsJson = JsonSerializer.SerializeToNode(state, _jsonSerializerOptions);
        await outbox.Append(key.Value.ToString()!, eventType, stateAsJson!.AsObject());
    }

    /// <inheritdoc/>
    public Task BeginReplay()
    {
        _replaying = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task EndReplay()
    {
        _replaying = false;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<ExpandoObject?> FindOrDefault(Key key, bool isReplaying)
    {
        if (_replaying) return new ExpandoObject();

        var eventType = _model.Schema.GetEventType();
        try
        {
            var lastInstance = await _eventSequenceStorageProvider.GetLastInstanceFor(EventSequenceId.Outbox, eventType.Id, key.Value.ToString()!);
            return lastInstance.Content;
        }
        catch (MissingEvent)
        {
            return default;
        }
    }

    /// <inheritdoc/>
    public Task PrepareInitialRun(bool isReplaying) => Task.CompletedTask;
}
