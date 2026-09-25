// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.DependencyInjection;
using IReadModelsService = Cratis.Chronicle.Contracts.ReadModels.IReadModels;

namespace Cratis.Chronicle.Statistics;

/// <summary>
/// Represents an implementation of <see cref="IStatisticsReader"/>.
/// </summary>
/// <param name="readModels"><see cref="IReadModelsService"/> to read materialized instances through.</param>
[Singleton]
public class StatisticsReader(IReadModelsService readModels) : IStatisticsReader
{
    /// <summary>
    /// The upper bound on rows read in one request.
    /// </summary>
    /// <remarks>
    /// One row per event type per namespace, so this is exceeded only by a store with tens of thousands of
    /// distinct event types - far past the point where a key figure is the right thing to be showing. Bounded
    /// rather than paged because the figures are folds over the whole set: a page of rows sums to a wrong total,
    /// which is worse than a total that refuses to grow beyond a stated limit.
    /// </remarks>
    public const int MaximumRows = 10_000;

    static readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc/>
    public async Task<IEnumerable<EventTypeStatistics>> GetEventTypeStatistics(EventStoreName eventStore, EventStoreNamespaceName @namespace)
    {
        var response = await readModels.GetInstances(new()
        {
            EventStore = eventStore,
            Namespace = @namespace,
            ReadModel = nameof(EventTypeStatistics),
            Page = 0,
            PageSize = MaximumRows
        });

        return response.Instances
            .Select(instance => JsonSerializer.Deserialize<EventTypeStatistics>(instance, _serializerOptions))
            .Where(row => row is not null)
            .Select(row => row!)
            .ToArray();
    }

    /// <inheritdoc/>
    public Task<IEnumerable<EventTypeStatistics>> GetEventTypeStatisticsForEventStore(EventStoreName eventStore) =>

        // The global half of the projection materializes against the event store rather than any one namespace,
        // which is what makes a store-wide total one read instead of a read per namespace.
        GetEventTypeStatistics(eventStore, EventStoreNamespaceName.NotSet);
}
