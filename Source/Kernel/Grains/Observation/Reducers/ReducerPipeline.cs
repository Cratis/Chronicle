// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Grains.Observation.Reducers;

/// <summary>
/// Represents an implementation of <see cref="IReducerPipeline"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ReducerPipeline"/> class.
/// </remarks>
/// <param name="readModel">The <see cref="ReadModelDefinition"/> the sink is for.</param>
/// <param name="sink"><see cref="ISink"/> to use in pipeline.</param>
/// <param name="objectComparer"><see cref="IObjectComparer"/> for comparing objects.</param>
public class ReducerPipeline(
    ReadModelDefinition readModel,
    ISink sink,
    IObjectComparer objectComparer) : IReducerPipeline
{
    /// <inheritdoc/>
    public ReadModelDefinition ReadModel { get; } = readModel;

    /// <inheritdoc/>
    public ISink Sink { get; } = sink;

    /// <inheritdoc/>
    public Task BeginReplay(ReplayContext context) => Sink.BeginReplay(context);

    /// <inheritdoc/>
    public Task EndReplay(ReplayContext context) => Sink.EndReplay(context);

    /// <inheritdoc/>
    public Task BeginBulk() => Sink.BeginBulk();

    /// <inheritdoc/>
    public Task EndBulk() => Sink.EndBulk();

    /// <inheritdoc/>
    public async Task Handle(ReducerContext context, ReducerDelegate reducer)
    {
        var initial = await Sink.FindOrDefault(context.Key);

        var result = await reducer(context.Events, initial);

        if (result.ObserverResult.State != ObserverSubscriberState.Ok) return;

        var changeset = new Changeset<AppendedEvent, ExpandoObject>(objectComparer, context.Events.First(), initial ?? new ExpandoObject());
        if (result.ReadModelState == null)
        {
            if (initial != null)
            {
                changeset.Add(new Removed(initial));
            }
        }
        else if (!objectComparer.Compare(initial, result.ReadModelState, out var differences))
        {
            changeset.Add(new PropertiesChanged<ExpandoObject>(null!, differences));
        }

        if (changeset.HasChanges)
        {
            var failedPartitions = await Sink.ApplyChanges(context.Key, changeset, context.Events.Last().Context.SequenceNumber);

            if (failedPartitions.Any())
            {
                var firstFailure = failedPartitions.First();
                throw new InvalidOperationException($"Bulk operation failed for partition {firstFailure.EventSourceId} at sequence number {firstFailure.EventSequenceNumber}");
            }
        }
    }
}
