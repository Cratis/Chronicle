// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Testing.Events;
using Cratis.Serialization;

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
        readModelType ??= typeof(TReadModel);
        var namingPolicy = new CamelCaseNamingPolicy();
        using var loggerFactory = new NullLoggerFactory();
        var activator = new ClientArtifactsActivator(store.ServiceProvider, loggerFactory);
        var hasExplicitProjection = store.ClientOptions.ExplicitArtifacts.Projections.Any(projection => projection.ReadModelType == readModelType);
        var reducerType = !hasExplicitProjection && store.Reducers.HasFor(readModelType)
            ? store.Reducers.GetHandlerForReadModelType(readModelType).ReducerType
            : null;
        var definition = ((Projections.Projections)store.Projections).Definitions.FirstOrDefault(definition =>
            definition.ReadModel == readModelType.GetReadModelIdentifier().Value);
        var fold = new ReadModelFold<TReadModel>
        {
            ReadModelType = readModelType,
            EventStore = store,
            EventTypes = store.EventTypes,
            SchemaGenerator = store.JsonSchemaGenerator,
            Activator = activator,
            ServiceProvider = store.ServiceProvider,
            NamingPolicy = namingPolicy,
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

    /// <summary>
    /// Rebuilds a collection for the runtime type accepted by IReadModels.
    /// </summary>
    /// <param name="store">The owning store.</param>
    /// <param name="readModelType">The read model type.</param>
    /// <param name="events">The ordered log snapshot.</param>
    /// <returns>The instances indexed by their materialized keys.</returns>
    internal static Task<IReadOnlyDictionary<EventSourceId, object>> Fold(EventStoreForTesting store, Type readModelType, IEnumerable<AppendedEvent> events) =>
        Fold<object>(store, events, true, readModelType);

    static ReadModelEvent ToInput(AppendedEvent e) => new(e.Context.EventSourceId, e.Content, e.Context.EventSourceType, e.Context.EventStreamType, e.Context.EventStreamId);
}
