// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Json;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Reduces each key independently on top of its seeded state.
/// </summary>
internal static class SeededReadModelReducer
{
    /// <summary>
    /// Processes seeded and unseeded keys, retaining seeds that receive no events.
    /// </summary>
    /// <typeparam name="TReadModel">The read model type.</typeparam>
    /// <param name="reducerType">The selected reducer.</param>
    /// <param name="events">The collected input.</param>
    /// <param name="context">The fold context.</param>
    /// <param name="initialState">The baseline for unseeded keys.</param>
    /// <param name="seeds">The per-key starting states.</param>
    /// <returns>The primary instance and every materialized key.</returns>
    public static async Task<(TReadModel? Primary, IReadOnlyDictionary<EventSourceId, TReadModel> Instances)> Process<TReadModel>(
        Type reducerType,
        IReadOnlyList<ReadModelEvent> events,
        ReadModelFoldContext context,
        TReadModel? initialState,
        IReadOnlyDictionary<EventSourceId, TReadModel> seeds)
        where TReadModel : class
    {
        var instances = seeds.ToDictionary(pair => pair.Key, pair => Clone(pair.Value)!);
        foreach (var group in events.Select((input, index) => (Input: input, Index: index)).GroupBy(pair => pair.Input.EventSourceId))
        {
            var current = instances.TryGetValue(group.Key, out var seed) ? seed : Clone(initialState);
            var inputs = group.Select(pair => new EventForEventSourceId(pair.Input.EventSourceId, pair.Input.Event, Causation.Unknown())
            {
                EventSourceType = pair.Input.EventSourceType,
                EventStreamType = pair.Input.EventStreamType,
                EventStreamId = pair.Input.EventStreamId,
                Tags = pair.Input.Tags
            });
            var groupContext = context with
            {
                EventContexts = context.EventContexts is null ? null : group.Select(pair => context.EventContexts[pair.Index]).ToArray()
            };
            var (reduced, _) = await ReducerReadModelProcessor.Process(reducerType, inputs, groupContext, current);
            if (reduced is null)
            {
                instances.Remove(group.Key);
            }
            else
            {
                instances[group.Key] = reduced;
            }
        }

        return (instances.Values.FirstOrDefault(), instances);
    }

    static TReadModel? Clone<TReadModel>(TReadModel? instance)
        where TReadModel : class =>
        instance is null ? null : JsonSerializer.Deserialize<TReadModel>(JsonSerializer.Serialize(instance, Globals.JsonSerializerOptions), Globals.JsonSerializerOptions);
}
