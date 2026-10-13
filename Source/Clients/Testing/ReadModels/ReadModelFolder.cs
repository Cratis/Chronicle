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
    /// <returns>The rebuilt instances indexed by their materialized keys.</returns>
    public static async Task<IReadOnlyDictionary<EventSourceId, TReadModel>> Fold<TReadModel>(EventStoreForTesting store, IEnumerable<AppendedEvent> events, bool useOriginalContexts)
        where TReadModel : class
    {
        var hasExplicitProjection = store.ClientOptions.ExplicitArtifacts.Projections.Any(projection => projection.ReadModelType == typeof(TReadModel));
        var reducerType = !hasExplicitProjection && store.Reducers.HasFor<TReadModel>()
            ? store.Reducers.GetHandlerForReadModelType(typeof(TReadModel)).ReducerType
            : null;
        var definition = ((Projections.Projections)store.Projections).Definitions.FirstOrDefault(definition =>
            definition.ReadModel == typeof(TReadModel).GetReadModelIdentifier().Value);
        var fold = new ReadModelFold<TReadModel>
        {
            EventStore = store,
            EventTypes = store.EventTypes,
            SchemaGenerator = store.JsonSchemaGenerator,
            ServiceProvider = store.ServiceProvider,
            ReducerType = reducerType,
            ProjectionDefinition = definition
        };
        var appended = events.ToArray();
        var inputs = appended.Select(ToInput).ToArray();
        if (reducerType is not null)
        {
            var instances = new Dictionary<EventSourceId, TReadModel>();
            foreach (var group in appended.GroupBy(e => e.Context.EventSourceId))
            {
                var (primary, _) = await fold.Process(group.Select(ToInput), useOriginalContexts ? group.Select(e => e.Context).ToArray() : null);
                if (primary is not null) instances[group.Key] = primary;
            }

            return instances;
        }

        var (_, projected) = await fold.Process(inputs, useOriginalContexts ? appended.Select(e => e.Context).ToArray() : null);
        return projected;
    }

    static ReadModelEvent ToInput(AppendedEvent e) => new(e.Context.EventSourceId, e.Content, e.Context.EventSourceType, e.Context.EventStreamType, e.Context.EventStreamId, e.Context.Tags.Select(tag => tag.Value).ToArray());
}
