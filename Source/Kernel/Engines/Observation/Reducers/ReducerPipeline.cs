// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Changes;
using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Engines.Sinks;
using Aksio.Cratis.Projections;

namespace Aksio.Cratis.Kernel.Engines.Observation.Reducers;

/// <summary>
/// Represents an implementation of <see cref="IReducerPipeline"/>.
/// </summary>
public class ReducerPipeline : IReducerPipeline
{
    readonly IObjectComparer _objectComparer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReducerPipeline"/> class.
    /// </summary>
    /// <param name="readModel">The <see cref="Model"/> the sink is for.</param>
    /// <param name="sink"><see cref="ISink"/> to use in pipeline.</param>
    /// <param name="objectComparer"><see cref="IObjectComparer"/> for comparing objects.</param>
    public ReducerPipeline(
        Model readModel,
        ISink sink,
        IObjectComparer objectComparer)
    {
        ReadModel = readModel;
        Sink = sink;
        _objectComparer = objectComparer;
    }

    /// <inheritdoc/>
    public Model ReadModel { get; }

    /// <inheritdoc/>
    public ISink Sink { get; }

    /// <inheritdoc/>
    public Task BeginReplay() => Sink.BeginReplay();

    /// <inheritdoc/>
    public Task EndReplay() => Sink.EndReplay();

    /// <inheritdoc/>
    public async Task Handle(ReducerContext context, ReducerDelegate reducer)
    {
        var initial = await Sink.FindOrDefault(context.Key);

        var reduced = await reducer(context.Events, initial);

        var changeset = new Changeset<AppendedEvent, ExpandoObject>(_objectComparer, context.Events.First(), initial ?? new ExpandoObject());
        if (!_objectComparer.Equals(initial, reduced, out var differences))
        {
            changeset.Add(new PropertiesChanged<ExpandoObject>(null!, differences));
        }
        await Sink.ApplyChanges(context.Key, changeset);
    }
}
