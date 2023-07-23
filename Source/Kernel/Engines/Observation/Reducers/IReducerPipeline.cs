// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Aksio.Cratis.Changes;
using Aksio.Cratis.Events;
using Aksio.Cratis.Json;
using Aksio.Cratis.Kernel.Engines.Projections;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Kernel.Engines.Observation.Reducers;

public delegate ExpandoObject Reducer(AppendedEvent @event, ExpandoObject initialState);

public interface IReducerPipeline
{
    /// <summary>
    /// Gets the <see cref="IProjectionSink">sink</see> to use for output.
    /// </summary>
    IProjectionSink Sink { get; }

    /// <summary>
    /// Handles the event and coordinates everything according to the pipeline.
    /// </summary>
    /// <param name="event"><see cref="AppendedEvent"/> to handle.</param>
    /// <param name="reducer"><see cref="Reducer"/> delegate.</param>
    /// <returns>Awaitable task.</returns>
    Task Handle(AppendedEvent @event, Reducer reducer);
}


public class ReducerPipeline : IReducerPipeline
{
    readonly IObjectComparer _objectComparer;
    readonly IExpandoObjectConverter _expandoObjectConverter;
    readonly JsonSerializerOptions _jsonSerializerOptions;

    public IProjectionSink Sink { get; }

    public ReducerPipeline(
        IProjectionSink sink,
        IObjectComparer objectComparer,
        IExpandoObjectConverter expandoObjectConverter,
        JsonSerializerOptions jsonSerializerOptions)
    {
        Sink = sink;
        _objectComparer = objectComparer;
        _expandoObjectConverter = expandoObjectConverter;
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    public async Task Handle(AppendedEvent @event, Reducer reducer)
    {
            // Get the current state from the sink
            var isReplaying = @event.Context.ObservationState.HasFlag(EventObservationState.Replay);

            // Resolve key through key resolvers
            var key = new Key(@event.Context.EventSourceId, ArrayIndexers.NoIndexers);
            var initial = await Sink.FindOrDefault(key, isReplaying);

            var initialAsJson = _expandoObjectConverter.ToJsonObject(initial, null!);

            var reduced = reducer(@event, initial);

            // Compare existing to new state and create a change set
            // On OK, apply changes to sink
            var changeset = new Changeset<ExpandoObject, ExpandoObject>(_objectComparer, reduced, initial);
            if (!_objectComparer.Equals(initial, reduced, out var differences))
            {
                changeset.Add(new PropertiesChanged<ExpandoObject>(null!, differences));
            }
            _projectionSink.Apply(key, changeset);


    }
}
