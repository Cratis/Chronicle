// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Sinks;
using Cratis.Models;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// /// Represents an implementation of <see cref="IProjectionBuilderFor{TModel}"/>.
/// </summary>
/// <typeparam name="TModel">Type of model.</typeparam>
public class ProjectionBuilderFor<TModel> : ProjectionBuilder<TModel, IProjectionBuilderFor<TModel>>, IProjectionBuilderFor<TModel>
{
    readonly ProjectionId _identifier;
    readonly IJsonSchemaGenerator _schemaGenerator;
    EventSequenceId _eventSequenceId = EventSequenceId.Log;
    bool _isRewindable = true;
    bool _isActive = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionBuilderFor{TModel}"/> class.
    /// </summary>
    /// <param name="identifier">The unique identifier for the projection.</param>
    /// <param name="modelNameResolver">The <see cref="IModelNameResolver"/> to use for naming the models.</param>
    /// <param name="eventTypes"><see cref="IEventTypes"/> for providing event type information.</param>
    /// <param name="schemaGenerator"><see cref="IJsonSchemaGenerator"/> for generating JSON schemas.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for any JSON serialization.</param>
    public ProjectionBuilderFor(
        ProjectionId identifier,
        IModelNameResolver modelNameResolver,
        IEventTypes eventTypes,
        IJsonSchemaGenerator schemaGenerator,
        JsonSerializerOptions jsonSerializerOptions)
        : base(eventTypes, schemaGenerator, jsonSerializerOptions, false)
    {
        _identifier = identifier;
        _schemaGenerator = schemaGenerator;
        _modelName = modelNameResolver.GetNameFor(typeof(TModel));
    }

    /// <inheritdoc/>
    public IProjectionBuilderFor<TModel> FromEventSequence(EventSequenceId eventSequenceId)
    {
        _eventSequenceId = eventSequenceId;
        return this;
    }

    /// <inheritdoc/>
    public IProjectionBuilderFor<TModel> ModelName(string modelName)
    {
        _modelName = modelName;
        return this;
    }

    /// <inheritdoc/>
    public IProjectionBuilderFor<TModel> NotRewindable()
    {
        _isRewindable = false;
        return this;
    }

    /// <inheritdoc/>
    public IProjectionBuilderFor<TModel> Passive()
    {
        _isActive = false;
        return this;
    }

    /// <summary>
    /// Build the projection definition.
    /// </summary>
    /// <returns><see cref="ProjectionDefinition"/>.</returns>
    internal ProjectionDefinition Build()
    {
        var modelType = typeof(TModel);
        var modelSchema = _schemaGenerator.Generate(modelType);

        return new()
        {
            EventSequenceId = _eventSequenceId,
            Identifier = _identifier,
            Model = new()
            {
                Name = _modelName,
                Schema = modelSchema.ToJson()
            },
            IsActive = _isActive,
            IsRewindable = _isRewindable,
            InitialModelState = _initialValues.ToJsonString(),
            From = _fromDefinitions,
            Join = _joinDefinitions,
            Children = _childrenDefinitions.ToDictionary(_ => (string)_.Key, _ => _.Value),
            All = _fromEveryDefinition,
            RemovedWith = _removedWithDefinitions,
            Sink = new()
            {
                ConfigurationId = Guid.Empty,
                TypeId = WellKnownSinkTypes.MongoDB
            }
        };
    }
}
