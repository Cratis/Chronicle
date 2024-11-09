// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Models;
using Cratis.Chronicle.Storage.Changes;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Grains.Observation.Reducers;

/// <summary>
/// Represents an implementation of <see cref="IReducerPipeline"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ReducerPipeline"/> class.
/// </remarks>
/// <param name="readModel">The <see cref="Model"/> the sink is for.</param>
/// <param name="sink"><see cref="ISink"/> to use in pipeline.</param>
/// <param name="objectComparer"><see cref="IObjectComparer"/> for comparing objects.</param>
public class ReducerPipeline(
    Model readModel,
    ISink sink,
    IObjectComparer objectComparer) : IReducerPipeline
{
    /// <inheritdoc/>
    public Model ReadModel { get; } = readModel;

    /// <inheritdoc/>
    public ISink Sink { get; } = sink;

    /// <inheritdoc/>
    public Task BeginReplay() => Sink.BeginReplay();

    /// <inheritdoc/>
    public Task EndReplay() => Sink.EndReplay();

    /// <inheritdoc/>
    public async Task Handle(ReducerContext context, ReducerDelegate reducer)
    {
        var initial = await Sink.FindOrDefault(context.Key);

        var reduced = await reducer(context.Events, initial);

        var changeset = new Changeset<AppendedEvent, ExpandoObject>(objectComparer, context.Events.First(), initial ?? new ExpandoObject());
        if (!objectComparer.Compare(initial, reduced, out var differences))
        {
            changeset.Add(new PropertiesChanged<ExpandoObject>(null!, differences));
        }

        if (changeset.HasChanges)
        {
            changeset.SetSequenceNumber();
            await Sink.ApplyChanges(context.Key, changeset);
        }
    }
}
