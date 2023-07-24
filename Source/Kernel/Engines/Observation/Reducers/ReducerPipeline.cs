// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Changes;
using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Engines.Projections;
using Aksio.Cratis.Kernel.Engines.Sinks;
using Aksio.Cratis.Projections;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Kernel.Engines.Observation.Reducers;

/// <summary>
/// Represents an implementation of <see cref="IReducerPipeline"/>.
/// </summary>
public class ReducerPipeline : IReducerPipeline
{
    readonly IObjectComparer _objectComparer;

    /// <inheritdoc/>
    public Model ReadModel { get; }

    /// <inheritdoc/>
    public IProjectionSink Sink { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReducerPipeline"/> class.
    /// </summary>
    /// <param name="readModel">The <see cref="Model"/> the sink is for.</param>
    /// <param name="sink"></param>
    /// /// <param name="objectComparer"></param>
    public ReducerPipeline(
        Model readModel,
        IProjectionSink sink,
        IObjectComparer objectComparer)
    {
        ReadModel = readModel;
        Sink = sink;
        _objectComparer = objectComparer;
    }

    /// <inheritdoc/>
    public async Task Handle(AppendedEvent @event, ReducerDelegate reducer)
    {
        var isReplaying = @event.Context.ObservationState.HasFlag(EventObservationState.Replay);
        var key = new Key(@event.Context.EventSourceId, ArrayIndexers.NoIndexers);
        var initial = await Sink.FindOrDefault(key, isReplaying);

        var reduced = await reducer(@event, initial);

        var changeset = new Changeset<AppendedEvent, ExpandoObject>(_objectComparer, @event, initial ?? new ExpandoObject());
        if (!_objectComparer.Equals(initial, reduced, out var differences))
        {
            changeset.Add(new PropertiesChanged<ExpandoObject>(null!, differences));
        }
        await Sink.ApplyChanges(key, changeset, isReplaying);
    }
}
