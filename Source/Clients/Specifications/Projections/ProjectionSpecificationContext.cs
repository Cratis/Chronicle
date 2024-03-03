// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.Changes;
using Aksio.Cratis.Compliance;
using Aksio.Cratis.Dynamic;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Json;
using Aksio.Cratis.Kernel.Contracts.Projections;
using Aksio.Cratis.Kernel.Keys;
using Aksio.Cratis.Kernel.Projections;
using Aksio.Cratis.Kernel.Projections.Expressions;
using Aksio.Cratis.Kernel.Projections.Expressions.EventValues;
using Aksio.Cratis.Kernel.Projections.Expressions.Keys;
using Aksio.Cratis.Kernel.Projections.Pipelines;
using Aksio.Cratis.Kernel.Storage.Changes;
using Aksio.Cratis.Kernel.Storage.EventSequences;
using Aksio.Cratis.Kernel.Storage.Sinks.InMemory;
using Aksio.Cratis.Models;
using Aksio.Cratis.Projections;
using Aksio.Cratis.Properties;
using Aksio.Cratis.Schemas;
using Aksio.Json;
using Aksio.Reflection;
using Aksio.Types;

namespace Aksio.Cratis.Specifications.Integration;

/// <summary>
/// Represents the context for specifications for a projection.
/// </summary>
/// <typeparam name="TModel">Type of target model the projection is for.</typeparam>
public class ProjectionSpecificationContext<TModel> : IHaveEventLog, IDisposable
{
    /// <summary>
    /// Gets the internal event log.
    /// </summary>
    internal readonly EventLogForSpecifications _eventLog;

    readonly IProjection _projection;
    readonly IEventSequenceStorage _eventSequenceStorage;
    readonly IProjectionPipeline _pipeline;
    readonly InMemorySink _sink;

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

        var builder = new ProjectionBuilderFor<TModel>(
            identifier.Value,
            new ModelNameResolver(new DefaultModelNameConvention()),
            GlobalsForSpecifications.EventTypes,
            schemaGenerator,
            Globals.JsonSerializerOptions);
        defineProjection(builder);
        Definition = builder.Build();

        var eventValueProviderExpressionResolvers = new EventValueProviderExpressionResolvers(typeFormats);

        _eventSequenceStorage = new EventSequenceStorageForSpecifications(_eventLog);
        var factory = new ProjectionFactory(
            new ModelPropertyExpressionResolvers(eventValueProviderExpressionResolvers, typeFormats),
            new EventValueProviderExpressionResolvers(typeFormats),
            new KeyExpressionResolvers(eventValueProviderExpressionResolvers),
            new ExpandoObjectConverter(typeFormats),
            new EventStoreNamespaceStorageForSpecifications(_eventSequenceStorage));

        // TODO: Circle back to this when we have conversions from Kernel definition to Proto definition of a projection definition
        _projection = factory.CreateFrom(null!).GetAwaiter().GetResult();

        var objectComparer = new ObjectComparer();

        _sink = new InMemorySink(_projection.Model, typeFormats);
        _pipeline = new ProjectionPipeline(
            _projection,
            _eventSequenceStorage,
            _sink,
            objectComparer,
            new NullChangesetStorage(),
            typeFormats,
            new NullLogger<ProjectionPipeline>());
    }

    /// <inheritdoc/>
    public IEventLog EventLog => _eventLog;

    /// <inheritdoc/>
    public IEnumerable<AppendedEventForSpecifications> AppendedEvents => _eventLog.AppendedEvents;

    /// <summary>
    /// Gets the <see cref="ProjectionDefinition"/> for the projection.
    /// </summary>
    public ProjectionDefinition Definition { get; }

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
        modelId ??= eventSourceId.Value;

        if (modelId?.GetType().IsAPrimitiveType() == false)
        {
            modelId = modelId.AsExpandoObject();
        }

        var key = new Key(modelId!, ArrayIndexers.NoIndexers);
        _sink.RemoveAnyExisting(key);

        var cursor = await _eventSequenceStorage.GetFromSequenceNumber(EventSequenceNumber.First, eventSourceId, _projection.EventTypes);
        while (await cursor.MoveNext())
        {
            foreach (var @event in cursor.Current)
            {
                await _pipeline.Handle(@event);
                projectedEventsCount++;
            }
        }

        var result = await _sink.FindOrDefault(key);
        result?.RemoveNulls();
        var json = JsonSerializer.Serialize(result, Globals.JsonSerializerOptions);
        return new(JsonSerializer.Deserialize<TModel>(json, Globals.JsonSerializerOptions)!, Array.Empty<PropertyPath>(), projectedEventsCount);
    }

    /// <summary>
    /// Gets the count of model instances that was affected within this projection context.
    /// </summary>
    /// <returns>The number of models affected.</returns>
    public int ModelCount() => _sink.Collection.Count;
}
