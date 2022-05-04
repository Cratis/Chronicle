// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.Changes;
using Aksio.Cratis.Events;
using Aksio.Cratis.Events.Projections;
using Aksio.Cratis.Events.Projections.Changes;
using Aksio.Cratis.Events.Projections.Definitions;
using Aksio.Cratis.Events.Projections.Expressions;
using Aksio.Cratis.Events.Projections.InMemory;
using Aksio.Cratis.Events.Projections.Pipelines;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Integration;
using Aksio.Cratis.Json;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Specifications.Integration;

/// <summary>
/// Represents a <see cref="IAdapterProjectionFor{T}"/> for in-memory purpose.
/// </summary>
/// <typeparam name="TModel">Type of model.</typeparam>
public class SpecificationAdapterProjectionFor<TModel> : IAdapterProjectionFor<TModel>, IDisposable
{
    readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
                {
                    new ConceptAsJsonConverterFactory()
                }
    };

    readonly IProjection _projection;
    readonly IEventSequenceStorageProvider _eventSequenceStorageProvider;
    readonly IProjectionPipeline _pipeline;
    readonly InMemoryProjectionSink _sink;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpecificationAdapterProjectionFor{TModel}"/> class.
    /// </summary>
    /// <param name="projectionDefinition">The <see cref="ProjectionDefinition"/> for the projection.</param>
    /// <param name="eventSequenceStorageProvider">The <see cref="IEventSequenceStorageProvider"/> to use.</param>
    /// <param name="comparer"><see cref="IObjectsComparer"/> for comparing differences.</param>
    public SpecificationAdapterProjectionFor(
        ProjectionDefinition projectionDefinition,
        IEventSequenceStorageProvider eventSequenceStorageProvider,
        IObjectsComparer comparer)
    {
        var factory = new ProjectionFactory(new PropertyMapperExpressionResolvers());
        var task = factory.CreateFrom(projectionDefinition);
        task.Wait();
        _projection = task.Result;
        _eventSequenceStorageProvider = eventSequenceStorageProvider;
        _sink = new InMemoryProjectionSink();
        _pipeline = new ProjectionPipeline(
            _projection,
            _eventSequenceStorageProvider,
            _sink,
            comparer,
            new NullChangesetStorage(),
            new NullLogger<ProjectionPipeline>());
    }

    /// <inheritdoc/>
    public void Dispose() => _sink.Dispose();

    /// <inheritdoc/>
    public async Task<TModel> GetById(EventSourceId eventSourceId)
    {
        var cursor = await _eventSequenceStorageProvider.GetFromSequenceNumber(EventSequenceNumber.First, eventSourceId, _projection.EventTypes);
        while (await cursor.MoveNext())
        {
            foreach (var @event in cursor.Current)
            {
                await _pipeline.Handle(@event);
            }
        }

        var result = await _sink.FindOrDefault(new(eventSourceId, ArrayIndexers.NoIndexers));
        var json = JsonSerializer.Serialize(result);
        return JsonSerializer.Deserialize<TModel>(json, _serializerOptions)!;
    }
}
