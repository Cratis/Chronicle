// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using Aksio.Cratis.Events.Projections.Definitions;
using Aksio.Cratis.Events.Schemas;
using Aksio.Cratis.Models;
using Aksio.Cratis.Properties;
using Aksio.Cratis.Reflection;
using Aksio.Cratis.Schemas;
using Aksio.Cratis.Strings;
using Humanizer;

namespace Aksio.Cratis.Events.Projections;

/// <summary>
/// /// Represents an implementation of <see cref="IProjectionBuilderFor{TModel}"/>.
/// </summary>
/// <typeparam name="TModel">Type of model.</typeparam>
public class ProjectionBuilderFor<TModel> : IProjectionBuilderFor<TModel>
{
    readonly ProjectionId _identifier;
    readonly IEventTypes _eventTypes;
    readonly IJsonSchemaGenerator _schemaGenerator;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    readonly Dictionary<EventType, FromDefinition> _fromDefinitions = new();
    readonly Dictionary<PropertyPath, ChildrenDefinition> _childrenDefinitions = new();
    readonly Dictionary<EventType, JoinDefinition> _joinDefinitions = new();
    bool _isRewindable = true;
    string _modelName;
    string? _name;
    JsonDocument? _initialValues;
    EventType? _removedWithEvent;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionBuilderFor{TModel}"/> class.
    /// </summary>
    /// <param name="identifier">The unique identifier for the projection.</param>
    /// <param name="eventTypes"><see cref="IEventTypes"/> for providing event type information.</param>
    /// <param name="schemaGenerator"><see cref="IJsonSchemaGenerator"/> for generating JSON schemas.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for any JSON serialization.</param>
    public ProjectionBuilderFor(
        ProjectionId identifier,
        IEventTypes eventTypes,
        IJsonSchemaGenerator schemaGenerator,
        JsonSerializerOptions jsonSerializerOptions)
    {
        _identifier = identifier;
        _eventTypes = eventTypes;
        _schemaGenerator = schemaGenerator;
        _jsonSerializerOptions = jsonSerializerOptions;
        if (typeof(TModel).HasAttribute<ModelNameAttribute>())
        {
            _modelName = typeof(TModel).GetCustomAttribute<ModelNameAttribute>(false)!.Name;
        }
        else
        {
            _modelName = typeof(TModel).Name.Pluralize().ToCamelCase();
        }
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
    public IProjectionBuilderFor<TModel> WithInitialValues(Func<TModel> initialValueProviderCallback)
    {
        var instance = initialValueProviderCallback();
        _initialValues = JsonSerializer.SerializeToDocument(instance, typeof(TModel), _jsonSerializerOptions);
        return this;
    }

    /// <inheritdoc/>
    public IProjectionBuilderFor<TModel> From<TEvent>(Action<IFromBuilder<TModel, TEvent>> builderCallback)
    {
        var builder = new FromBuilder<TModel, TEvent>();
        builderCallback(builder);
        var eventType = _eventTypes.GetEventTypeFor(typeof(TEvent));
        _fromDefinitions[eventType] = builder.Build();
        return this;
    }

    /// <inheritdoc/>
    public IProjectionBuilderFor<TModel> Join<TEvent>(Action<IJoinBuilder<TModel, TEvent>> builderCallback)
    {
        var builder = new JoinBuilder<TModel, TEvent>();
        builderCallback(builder);
        var eventType = _eventTypes.GetEventTypeFor(typeof(TEvent));
        _joinDefinitions[eventType] = builder.Build();
        return this;
    }

    /// <inheritdoc/>
    public IProjectionBuilderFor<TModel> RemovedWith<TEvent>()
    {
        if (_removedWithEvent != default)
        {
            throw new RemovalAlreadyDefined(_identifier);
        }

        _removedWithEvent = _eventTypes.GetEventTypeFor(typeof(TEvent));
        return this;
    }

    /// <inheritdoc/>
    public IProjectionBuilderFor<TModel> Children<TChildModel>(Expression<Func<TModel, IEnumerable<TChildModel>>> targetProperty, Action<IChildrenBuilder<TModel, TChildModel>> builderCallback)
    {
        var builder = new ChildrenBuilder<TModel, TChildModel>(_eventTypes, _schemaGenerator, _jsonSerializerOptions);
        builderCallback(builder);
        _childrenDefinitions[targetProperty.GetPropertyPath()] = builder.Build();
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

        return new ProjectionDefinition(
            _identifier,
            _name ?? modelType.FullName ?? "[N/A]",
            new ModelDefinition(_modelName, modelSchema.ToJson()),
            _isRewindable,
            _initialValues ?? JsonDocument.Parse("{}"),
            _fromDefinitions,
            _joinDefinitions,
            _childrenDefinitions,
            _removedWithEvent == default ? default : new RemovedWithDefinition(_removedWithEvent));
    }
}
