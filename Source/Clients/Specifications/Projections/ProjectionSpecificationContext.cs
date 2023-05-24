// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Changes;
using Aksio.Compliance;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Json;
using Aksio.Cratis.Kernel.Engines.Projections;
using Aksio.Cratis.Kernel.Engines.Projections.Changes;
using Aksio.Cratis.Kernel.Engines.Projections.Expressions;
using Aksio.Cratis.Kernel.Engines.Projections.Expressions.EventValues;
using Aksio.Cratis.Kernel.Engines.Projections.Expressions.Keys;
using Aksio.Cratis.Kernel.Engines.Projections.InMemory;
using Aksio.Cratis.Kernel.Engines.Projections.Pipelines;
using Aksio.Models;
using Aksio.Cratis.Projections;
using Aksio.Properties;
using Aksio.Cratis.Schemas;
using Aksio.Cratis.Specifications.Types;

namespace Aksio.Cratis.Specifications.Integration;

/// <summary>
/// Represents the context for specifications for a projection.
/// </summary>
/// <typeparam name="TModel">Type of target model the projection is for.</typeparam>
public class ProjectionSpecificationContext<TModel> : IHaveEventLog, IDisposable
{
    internal readonly EventLogForSpecifications _eventLog;

    /// <inheritdoc/>
    public IEventLog EventLog => _eventLog;

    /// <inheritdoc/>
    public IEnumerable<AppendedEventForSpecifications> AppendedEvents => _eventLog.AppendedEvents;

    readonly IProjection _projection;
    readonly IEventSequenceStorage _eventSequenceStorage;
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

        var typeFormats = new TypeFormats();
        var expandoObjectConverter = new ExpandoObjectConverter(typeFormats);
        _eventLog = new(expandoObjectConverter, schemaGenerator);

        var builder = new ProjectionBuilderFor<TModel>(identifier.Value, new DefaultModelNameConvention(), new EventTypesForSpecifications(), schemaGenerator, Globals.JsonSerializerOptions);
        defineProjection(builder);
        var projectionDefinition = builder.Build();

        var eventValueProviderExpressionResolvers = new EventValueProviderExpressionResolvers(typeFormats);

        var factory = new ProjectionFactory(
            new ModelPropertyExpressionResolvers(eventValueProviderExpressionResolvers, typeFormats),
            new KeyExpressionResolvers(eventValueProviderExpressionResolvers),
            new ExpandoObjectConverter(typeFormats),
            new EventSequenceStorageProviderForSpecifications(_eventLog));
        _projection = factory.CreateFrom(projectionDefinition).GetAwaiter().GetResult();

        var objectsComparer = new ObjectsComparer();

        _eventSequenceStorage = new EventSequenceStorageProviderForSpecifications(_eventLog);
        _sink = new InMemoryProjectionSink(_projection.Model, typeFormats, objectsComparer);
        _pipeline = new ProjectionPipeline(
            _projection,
            _eventSequenceStorage,
            _sink,
            objectsComparer,
            new NullChangesetStorage(),
            typeFormats,
            new NullLogger<ProjectionPipeline>());
    }

    /// <inheritdoc/>
    public void Dispose() => _sink.Dispose();

    /// <summary>
    /// Get a specific instance from the projection.
    /// </summary>
    /// <param name="eventSourceId">The identifier.</param>
    /// <param name="modelId">Optional model identifier. Use this if the projection has a key definition other than event source id.</param>
    /// <returns>Instance of the model.</returns>
    /// <remarks>
    /// The reason the event source identifier has to be there is that the event store does not support querying into.
    /// </remarks>
    public async Task<ProjectionResult<TModel>> GetById(EventSourceId eventSourceId, object? modelId = null)
    {
        var projectedEventsCount = 0;
        modelId ??= eventSourceId;
        var cursor = await _eventSequenceStorage.GetFromSequenceNumber(EventSequenceId.Log, EventSequenceNumber.First, eventSourceId, _projection.EventTypes);
        while (await cursor.MoveNext())
        {
            foreach (var @event in cursor.Current)
            {
                await _pipeline.Handle(@event);
                projectedEventsCount++;
            }
        }

        var result = await _sink.FindOrDefault(new(modelId, ArrayIndexers.NoIndexers), false);
        var json = JsonSerializer.Serialize(result, Globals.JsonSerializerOptions);
        return new(JsonSerializer.Deserialize<TModel>(json, Globals.JsonSerializerOptions)!, Array.Empty<PropertyPath>(), projectedEventsCount);
    }

    /// <summary>
    /// Gets the count of model instances that was affected within this projection context.
    /// </summary>
    /// <returns>The number of models affected.</returns>
    public int ModelCount() => _sink.Collection.Count;
}
