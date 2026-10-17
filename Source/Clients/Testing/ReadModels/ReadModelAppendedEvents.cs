// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Execution;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Orders contextual scenario input and isolates each reducer's event source without changing projection keys.
/// </summary>
internal static class ReadModelAppendedEvents
{
    /// <summary>
    /// Folds collected input, preserving appended contexts and synthesizing only bare-event contexts.
    /// </summary>
    /// <typeparam name="TReadModel">The read model type.</typeparam>
    /// <param name="fold">The configured projection or reducer fold.</param>
    /// <param name="events">All collected scenario input.</param>
    /// <returns>The primary instance and every resolved instance.</returns>
    public static async Task<(TReadModel? Primary, IReadOnlyDictionary<EventSourceId, TReadModel> Instances)> Process<TReadModel>(
        ReadModelFold<TReadModel> fold,
        IEnumerable<ReadModelEvent> events)
        where TReadModel : class
    {
        var ordered = events.Select((input, index) => input with { Context = input.Context ?? SyntheticContext(fold, input, index) })
            .OrderBy(input => input.Context!.SequenceNumber.Value)
            .ToArray();
        if (fold.ReducerType is null)
        {
            return await ProcessGroup(fold, ordered);
        }

        var instances = new Dictionary<EventSourceId, TReadModel>();
        foreach (var group in ordered.GroupBy(input => input.EventSourceId))
        {
            var (primary, _) = await ProcessGroup(fold, group.ToArray());
            if (primary is not null) instances[group.Key] = primary;
        }

        return (instances.Values.FirstOrDefault(), instances);
    }

    static Task<(TReadModel? Primary, IReadOnlyDictionary<EventSourceId, TReadModel> Instances)> ProcessGroup<TReadModel>(
        ReadModelFold<TReadModel> fold,
        IReadOnlyList<ReadModelEvent> events)
        where TReadModel : class =>
        fold.Process(events, events.Select(input => input.Context!).ToArray(), new EventSequenceId($"testing-appended-input-{Guid.NewGuid()}"));

    static EventContext SyntheticContext<TReadModel>(ReadModelFold<TReadModel> fold, ReadModelEvent input, int index)
        where TReadModel : class =>
        EventContext.From(
            fold.EventStore.Name,
            fold.EventStore.Namespace,
            fold.EventTypes.GetEventTypeFor(input.Event.GetType()),
            input.EventSourceType,
            input.EventSourceId,
            input.EventStreamType,
            input.EventStreamId,
            (ulong)index,
            CorrelationId.NotSet) with
        {
            Tags = input.Tags.Select(tag => new Tag(tag)).ToArray()
        };
}
