// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventTypes;
using Cratis.Chronicle.Schemas;
using Cratis.Models;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// /// Represents an implementation of <see cref="IProjectionBuilderFor{TModel}"/>.
/// </summary>
/// <typeparam name="TModel">Type of model.</typeparam>
public class ProjectionBuilderFor<TModel> : ProjectionBuilder<TModel, IProjectionBuilderFor<TModel>>, IProjectionBuilderFor<TModel>
{
    readonly ProjectionId _identifier;
    readonly IEventTypes _eventTypes;
    readonly IJsonSchemaGenerator _schemaGenerator;
    bool _isRewindable = true;

    string? _name;

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
        : base(eventTypes, schemaGenerator, jsonSerializerOptions)
    {
        _identifier = identifier;
        _eventTypes = eventTypes;
        _schemaGenerator = schemaGenerator;
        _modelName = modelNameResolver.GetNameFor(typeof(TModel));
    }

    /// <inheritdoc/>
    public IProjectionBuilderFor<TModel> WithName(string name)
    {
        _name = name;
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
    public ProjectionDefinition Build()
    {
        var modelType = typeof(TModel);
        var modelSchema = _schemaGenerator.Generate(modelType);
        if (_eventTypes.HasFor(modelType))
        {
            modelSchema.SetEventType(_eventTypes.GetEventTypeFor(modelType));
        }

        return new()
        {
            Identifier = _identifier,
            Name = _name ?? modelType.FullName ?? "[N/A]",
            Model = new()
            {
                Name = _modelName,
                Schema = modelSchema.ToJson()
            },
            IsActive = true,
            IsRewindable = _isRewindable,
            InitialModelState = _initialValues.ToJsonString(),
            From = _fromDefinitions,
            Join = _joinDefinitions,
            Children = _childrenDefinitions.ToDictionary(_ => (string)_.Key, _ => _.Value),
            All = _allDefinition,
            RemovedWith = _removedWithEvent == default ? default : new() { Event = _removedWithEvent }
        };
    }
}
