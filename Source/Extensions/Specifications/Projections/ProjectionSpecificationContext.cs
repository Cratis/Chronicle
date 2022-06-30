// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.Changes;
using Aksio.Cratis.Compliance;
using Aksio.Cratis.Events;
using Aksio.Cratis.Events.Projections;
using Aksio.Cratis.Events.Projections.Changes;
using Aksio.Cratis.Events.Projections.Expressions;
using Aksio.Cratis.Events.Projections.InMemory;
using Aksio.Cratis.Events.Projections.Pipelines;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Json;
using Aksio.Cratis.Properties;
using Aksio.Cratis.Schemas;
using Aksio.Cratis.Specifications.Types;

namespace Aksio.Cratis.Specifications.Integration;

/// <summary>
/// Represents the context for specifications for a projection.
/// </summary>
/// <typeparam name="TModel">Type of target model the projection is for.</typeparam>
public class ProjectionSpecificationContext<TModel> : IHaveEventLog, IDisposable
{
    internal readonly EventLogForSpecifications _eventLog = new();
    readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
                {
                    new ConceptAsJsonConverterFactory()
                }
    };

    /// <inheritdoc/>
    public IEventLog EventLog => _eventLog;

    /// <inheritdoc/>
    public IEnumerable<AppendedEventForSpecifications> AppendedEvents => _eventLog.AppendedEvents;

    readonly IProjection _projection;
    readonly IEventSequenceStorageProvider _eventSequenceStorageProvider;
    readonly IProjectionPipeline _pipeline;
    readonly InMemoryProjectionSink _sink;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionSpecificationContext{TModel}"/> class.
    /// </summary>
    /// <param name="identifier">The identifier of the projection.</param>
    /// <param name="defineProjection">An action to call for building the projection.</param>
    public ProjectionSpecificationContext(ProjectionId identifier, Action<IProjectionBuilderFor<TModel>> defineProjection)
    {
        var schemaGenerator = new JsonSchemaGenerator(
            new ComplianceMetadataResolver(
                new KnownInstancesOf<ICanProvideComplianceMetadataForType>(),
                new KnownInstancesOf<ICanProvideComplianceMetadataForProperty>()));

        var builder = new ProjectionBuilderFor<TModel>(identifier.Value, new EventTypesForSpecifications(), schemaGenerator);
        defineProjection(builder);
        var projectionDefinition = builder.Build();

        var factory = new ProjectionFactory(new PropertyMapperExpressionResolvers());
        _projection = factory.CreateFrom(projectionDefinition).GetAwaiter().GetResult();

        var objectsComparer = new ObjectsComparer();

        _eventSequenceStorageProvider = new EventSequenceStorageProviderForSpecifications(_eventLog);
        _sink = new InMemoryProjectionSink();
        _pipeline = new ProjectionPipeline(
            _projection,
            _eventSequenceStorageProvider,
            _sink,
            objectsComparer,
            new NullChangesetStorage(),
            new NullLogger<ProjectionPipeline>());
    }

    /// <inheritdoc/>
    public void Dispose() => _sink.Dispose();

    /// <summary>
    /// Get a specific instance from the projection.
    /// </summary>
    /// <param name="eventSourceId">The identifier.</param>
    /// <returns>Instance of the model.</returns>
    public async Task<ProjectionResult<TModel>> GetById(EventSourceId eventSourceId)
    {
        var projectedEventsCount = 0;
        var cursor = await _eventSequenceStorageProvider.GetFromSequenceNumber(EventSequenceId.Log, EventSequenceNumber.First, eventSourceId, _projection.EventTypes);
        while (await cursor.MoveNext())
        {
            foreach (var @event in cursor.Current)
            {
                await _pipeline.Handle(@event);
                projectedEventsCount++;
            }
        }

        var result = await _sink.FindOrDefault(new(eventSourceId, ArrayIndexers.NoIndexers));
        var json = JsonSerializer.Serialize(result);
        return new(JsonSerializer.Deserialize<TModel>(json, _serializerOptions)!, Array.Empty<PropertyPath>(), projectedEventsCount);
    }
}
