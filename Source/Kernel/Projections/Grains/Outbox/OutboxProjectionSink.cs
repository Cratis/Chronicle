// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Aksio.Cratis.Changes;
using Aksio.Cratis.Dynamic;
using Aksio.Cratis.Events.Schemas;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Events.Store.Grains;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Json;
using Aksio.Cratis.Objects;
using Orleans;

namespace Aksio.Cratis.Events.Projections.Grains.Outbox;

/// <summary>
/// Represents an implementation of <see cref="IProjectionSink"/> for projecting to the event outbox, MongoDB based.
/// </summary>
public class OutboxProjectionSink : IProjectionSink, IDisposable
{
    readonly Model _model;
    readonly IEventSequenceStorageProvider _eventSequenceStorageProvider;
    readonly IExecutionContextManager _executionContextManager;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    readonly IGrainFactory _grainFactory;
    bool _replaying;

    /// <inheritdoc/>
    public ProjectionSinkTypeId TypeId => WellKnownProjectionSinkTypes.Outbox;

    /// <inheritdoc/>
    public ProjectionSinkTypeName Name => "MongoDB Outbox";

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxProjectionSink"/> class.
    /// </summary>
    /// <param name="model"><see cref="Model"/> the sink is for.</param>
    /// <param name="eventSequenceStorageProvider">The <see cref="IEventSequenceStorageProvider"/>.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="jsonSerializerOptions">The global serialization options.</param>
    /// <param name="grainFactory"><see cref="IGrainFactory"/> for getting grains.</param>
    public OutboxProjectionSink(
        Model model,
        IEventSequenceStorageProvider eventSequenceStorageProvider,
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
    public async Task ApplyChanges(Key key, IChangeset<AppendedEvent, ExpandoObject> changeset)
    {
        var state = changeset.InitialState.Clone();
        foreach (var change in changeset.Changes)
        {
            state = state.OverwriteWith((change.State as ExpandoObject)!);
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
    public async Task<ExpandoObject> FindOrDefault(Key key)
    {
        if (_replaying) return new ExpandoObject();

        var eventType = _model.Schema.GetEventType();
        try
        {
            var lastInstance = await _eventSequenceStorageProvider.GetLastInstanceFor(EventSequenceId.Outbox, eventType.Id, key.Value.ToString()!);
            return lastInstance.Content.AsExpandoObject((value) =>
                value switch
                {
                    short => double.Parse(value.ToString()!),
                    int => double.Parse(value.ToString()!),
                    long => double.Parse(value.ToString()!),
                    ushort => double.Parse(value.ToString()!),
                    uint => double.Parse(value.ToString()!),
                    ulong => double.Parse(value.ToString()!),
                    float => double.Parse(value.ToString()!),
                    _ => value
                });
        }
        catch
        {
            return new ExpandoObject();
        }
    }

    /// <inheritdoc/>
    public Task PrepareInitialRun() => Task.CompletedTask;
}
