// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Testing.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Folds scenario events using the same projection and reducer processors for scenarios and observations.
/// </summary>
internal static class ReadModelFolder
{
    /// <summary>
    /// Rebuilds a collection, reducing each event source separately.
    /// </summary>
    /// <typeparam name="TReadModel">The read model type.</typeparam>
    /// <param name="store">The store supplying discovered artifacts.</param>
    /// <param name="events">The ordered log snapshot.</param>
    /// <param name="useOriginalContexts">Whether the events belong to this store's log.</param>
    /// <param name="readModelType">Optional runtime type when folding through the non-generic read-model surface.</param>
    /// <returns>The rebuilt instances indexed by their materialized keys.</returns>
    public static async Task<IReadOnlyDictionary<EventSourceId, TReadModel>> Fold<TReadModel>(EventStoreForTesting store, IEnumerable<AppendedEvent> events, bool useOriginalContexts, Type? readModelType = null)
        where TReadModel : class
    {
        var fold = CreateFold<TReadModel>(store, readModelType ?? typeof(TReadModel));
        var appended = events.ToArray();
        if (fold.ReducerType is not null)
        {
            return await ReduceEventSources(fold, appended, useOriginalContexts);
        }

        var (_, projected) = await fold.Process(appended.Select(ToInput), OriginalContexts(appended, useOriginalContexts));
        return projected;
    }

    /// <summary>
    /// Rebuilds a collection for the runtime type accepted by IReadModels.
    /// </summary>
    /// <param name="store">The owning store.</param>
    /// <param name="readModelType">The read model type.</param>
    /// <param name="events">The ordered log snapshot.</param>
    /// <returns>The instances indexed by their materialized keys.</returns>
    internal static Task<IReadOnlyDictionary<EventSourceId, object>> Fold(EventStoreForTesting store, Type readModelType, IEnumerable<AppendedEvent> events) =>
        Fold<object>(store, events, true, readModelType);

    static ReadModelFold<TReadModel> CreateFold<TReadModel>(EventStoreForTesting store, Type readModelType)
        where TReadModel : class =>
        new()
        {
            ReadModelType = readModelType,
            EventStore = store,
            EventTypes = store.EventTypes,
            SchemaGenerator = store.JsonSchemaGenerator,
            ServiceProvider = store.ServiceProvider,
            ReducerType = FindReducerType(store, readModelType),
            ProjectionDefinition = ((Projections.Projections)store.Projections).Definitions.FirstOrDefault(definition =>
                definition.ReadModel == readModelType.GetReadModelIdentifier().Value)
        };

    static Type? FindReducerType(EventStoreForTesting store, Type readModelType)
    {
        if (store.ClientOptions.ExplicitArtifacts.Projections.Any(projection => projection.ReadModelType == readModelType)) return null;
        if (!store.Reducers.HasFor(readModelType)) return null;

        return store.Reducers.GetHandlerForReadModelType(readModelType).ReducerType;
    }

    static async Task<IReadOnlyDictionary<EventSourceId, TReadModel>> ReduceEventSources<TReadModel>(
        ReadModelFold<TReadModel> fold,
        IEnumerable<AppendedEvent> events,
        bool useOriginalContexts)
        where TReadModel : class
    {
        var instances = new Dictionary<EventSourceId, TReadModel>();
        foreach (var group in events.GroupBy(e => e.Context.EventSourceId))
        {
            var (primary, _) = await fold.Process(group.Select(ToInput), OriginalContexts(group, useOriginalContexts));
            if (primary is not null) instances[group.Key] = primary;
        }

        return instances;
    }

    static EventContext[]? OriginalContexts(IEnumerable<AppendedEvent> events, bool useOriginalContexts) =>
        useOriginalContexts ? events.Select(e => e.Context).ToArray() : null;

    static ReadModelEvent ToInput(AppendedEvent e) => new(e.Context.EventSourceId, e.Content, e.Context.EventSourceType, e.Context.EventStreamType, e.Context.EventStreamId, e.Context.Tags.Select(tag => tag.Value).ToArray());
}
