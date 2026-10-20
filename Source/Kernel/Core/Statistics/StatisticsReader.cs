// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Namespaces;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using IReadModelsService = Cratis.Chronicle.Contracts.ReadModels.IReadModels;

namespace Cratis.Chronicle.Statistics;

/// <summary>
/// Represents an implementation of <see cref="IStatisticsReader"/>.
/// </summary>
/// <param name="readModels"><see cref="IReadModelsService"/> to read materialized instances through.</param>
/// <param name="grainFactory">The grain factory for discovering contributing namespaces.</param>
/// <param name="logger">The logger.</param>
[Singleton]
public class StatisticsReader(IReadModelsService readModels, IGrainFactory grainFactory, ILogger<StatisticsReader> logger) : IStatisticsReader
{
    /// <summary>
    /// The upper bound on rows read per namespace in one request. Reaching it fails the read rather than truncating totals.
    /// </summary>
    public const int MaximumRows = 10_000;

    /// <summary>
    /// The maximum number of concurrent namespace reads. Eight keeps store-wide queries from flooding storage.
    /// </summary>
    public const int MaximumConcurrentNamespaceReads = 8;

    static readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Initializes a new instance of the <see cref="StatisticsReader"/> class without logging.
    /// </summary>
    /// <param name="readModels">The read model service.</param>
    /// <param name="grainFactory">The grain factory.</param>
    public StatisticsReader(IReadModelsService readModels, IGrainFactory grainFactory)
        : this(readModels, grainFactory, NullLogger<StatisticsReader>.Instance)
    {
    }

    /// <inheritdoc/>
    public Task<IEnumerable<EventTypeStatistics>> GetEventTypeStatistics(EventStoreName eventStore, EventStoreNamespaceName @namespace) =>
        GetEventTypeStatistics(eventStore, @namespace, CancellationToken.None);

    /// <inheritdoc/>
    public async Task<IEnumerable<EventTypeStatistics>> GetEventTypeStatistics(EventStoreName eventStore, EventStoreNamespaceName @namespace, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var request = new Contracts.ReadModels.GetInstancesRequest()
        {
            EventStore = eventStore,
            Namespace = @namespace,
            ReadModel = nameof(EventTypeStatistics),
            Page = 0,
            PageSize = MaximumRows
        };
        var response = await readModels.GetInstances(request, cancellationToken).WaitAsync(cancellationToken);

        if (response.Instances.Count >= MaximumRows || response.TotalCount > MaximumRows)
        {
            logger.StatisticsReadLimitReached(eventStore, @namespace, MaximumRows, response.Instances.Count, response.TotalCount);
            throw new InvalidOperationException($"Statistics for event store '{eventStore}', namespace '{@namespace}' reached the {MaximumRows} row limit. Complete totals cannot be returned.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        return response.Instances
            .Select(ToRow)
            .Where(row => row is not null)
            .Select(row => row!)
            .ToArray();
    }

    /// <inheritdoc/>
    public Task<IEnumerable<EventTypeStatistics>> GetEventTypeStatisticsForEventStore(EventStoreName eventStore) =>
        GetEventTypeStatisticsForEventStore(eventStore, CancellationToken.None);

    /// <inheritdoc/>
    public async Task<IEnumerable<EventTypeStatistics>> GetEventTypeStatisticsForEventStore(EventStoreName eventStore, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var namespaces = (await grainFactory.GetGrain<INamespaces>(eventStore).GetAll().WaitAsync(cancellationToken)).ToArray();
        var rows = new IEnumerable<EventTypeStatistics>[namespaces.Length];
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = MaximumConcurrentNamespaceReads,
            CancellationToken = cancellationToken
        };
        await Parallel.ForEachAsync(
            Enumerable.Range(0, namespaces.Length),
            options,
            async (index, token) => rows[index] = await GetEventTypeStatistics(eventStore, namespaces[index], token));
        return rows.SelectMany(_ => _).ToArray();
    }

    /// <summary>
    /// Read one statistics row from the JSON of a read model instance.
    /// </summary>
    /// <param name="instance">The instance as JSON.</param>
    /// <returns>The row, or null if the instance is not one.</returns>
    /// <remarks>
    /// The event type and namespace make up the composite key of the row, and a sink keeps a key as the document
    /// identifier rather than as properties of their own - so they are read from the identifier when the instance
    /// does not carry them at the top level. Reading only the top level gave every row an empty event type and
    /// namespace, which folded every event into a single type in a single namespace.
    /// </remarks>
    internal static EventTypeStatistics? ToRow(string instance)
    {
        var row = JsonSerializer.Deserialize<EventTypeStatistics>(instance, _serializerOptions);
        if (row is null || (row.EventType.Length > 0 && row.Namespace.Length > 0))
        {
            return row;
        }

        using var document = JsonDocument.Parse(instance);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.Object || !IsIdentifier(property.Name))
            {
                continue;
            }

            var key = property.Value.Deserialize<EventTypeStatistics>(_serializerOptions);
            if (key is null)
            {
                continue;
            }

            if (row.EventType.Length == 0)
            {
                row.EventType = key.EventType;
            }

            if (row.Namespace.Length == 0)
            {
                row.Namespace = key.Namespace;
            }
        }

        return row;
    }

    static bool IsIdentifier(string name) =>
        name.Equals("_id", StringComparison.Ordinal) || name.Equals("id", StringComparison.OrdinalIgnoreCase);
}
