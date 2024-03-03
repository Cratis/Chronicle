// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Aksio.Cratis.Auditing;
using Aksio.Cratis.Changes;
using Aksio.Cratis.Dynamic;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.EventTypes;
using Aksio.Cratis.Identities;
using Aksio.Cratis.Kernel.Grains.EventSequences;
using Aksio.Cratis.Kernel.Keys;
using Aksio.Cratis.Kernel.Storage.EventSequences;
using Aksio.Cratis.Kernel.Storage.Sinks;
using Aksio.Cratis.Objects;
using Aksio.Cratis.Projections;
using Aksio.Cratis.Sinks;

namespace Aksio.Cratis.Kernel.Grains.Sinks.Outbox;

/// <summary>
/// Represents an implementation of <see cref="ISink"/> for projecting to the event outbox, MongoDB based.
/// </summary>
public class OutboxSink : ISink, IDisposable
{
    /// <summary>
    /// The <see cref="CausationType"/> for the sink.
    /// </summary>
    public static readonly CausationType CausationType = new("Outbox Projection Sink");
    readonly EventStoreName _eventStore;
    readonly EventStoreNamespaceName _namespace;
    readonly Model _model;
    readonly IEventSequenceStorage _eventSequenceStorage;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    readonly IGrainFactory _grainFactory;
    bool _replaying;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxSink"/> class.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> the sink is for.</param>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> the sink is for.</param>
    /// <param name="model"><see cref="Model"/> the sink is for.</param>
    /// <param name="eventSequenceStorage">The <see cref="IEventSequenceStorage"/>.</param>
    /// <param name="jsonSerializerOptions">The global serialization options.</param>
    /// <param name="grainFactory"><see cref="IGrainFactory"/> for getting grains.</param>
    public OutboxSink(
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        Model model,
        IEventSequenceStorage eventSequenceStorage,
        JsonSerializerOptions jsonSerializerOptions,
        IGrainFactory grainFactory)
    {
        _eventStore = eventStore;
        _namespace = @namespace;
        _model = model;
        _eventSequenceStorage = eventSequenceStorage;
        _jsonSerializerOptions = jsonSerializerOptions;
        _grainFactory = grainFactory;
    }

    /// <inheritdoc/>
    public SinkTypeId TypeId => WellKnownSinkTypes.Outbox;

    /// <inheritdoc/>
    public SinkTypeName Name => "MongoDB Outbox";

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
            state = state.MergeWith((change.State as ExpandoObject)!);
        }

        var eventType = _model.Schema.GetEventType();
        var outbox = _grainFactory.GetGrain<IEventSequence>(
            EventSequenceId.Outbox,
            keyExtension: new EventSequenceKey(
                (string)_eventStore,
                (string)_namespace));

        var stateAsJson = JsonSerializer.SerializeToNode(state, _jsonSerializerOptions);

        var causation = new Causation(
                    DateTimeOffset.UtcNow,
                    CausationType,
                    new Dictionary<string, string>()
                    {
                        { "key", key.ToString() },
                        { "event", eventType.Id.ToString() },
                        { "eventSequence", EventSequenceId.Outbox.ToString() }
                    });

        await outbox.Append(
            key.Value.ToString()!,
            eventType,
            stateAsJson!.AsObject(),
            new Causation[] { causation },
            Identity.System);
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
    public async Task<ExpandoObject?> FindOrDefault(Key key)
    {
        if (_replaying) return new ExpandoObject();

        var eventType = _model.Schema.GetEventType();
        try
        {
            var lastInstance = await _eventSequenceStorage.GetLastInstanceFor(eventType.Id, key.Value.ToString()!);
            return lastInstance.Content;
        }
        catch (MissingEvent)
        {
            return default;
        }
    }

    /// <inheritdoc/>
    public Task PrepareInitialRun() => Task.CompletedTask;
}
