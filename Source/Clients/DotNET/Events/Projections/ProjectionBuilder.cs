// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aksio.Cratis.Events.Projections.Definitions;
using Aksio.Cratis.Models;
using Aksio.Cratis.Properties;
using Aksio.Cratis.Reflection;
using Aksio.Cratis.Schemas;
using Aksio.Cratis.Strings;
using Humanizer;

namespace Aksio.Cratis.Events.Projections;

public class ProjectionBuilder<TModel, TBuilder> : IProjectionBuilder<TModel, TBuilder>
    where TBuilder : class
{
    protected readonly IEventTypes _eventTypes;
    protected readonly IJsonSchemaGenerator _schemaGenerator;
    protected readonly JsonSerializerOptions _jsonSerializerOptions;
    protected readonly Dictionary<EventType, FromDefinition> _fromDefinitions = new();
    protected readonly Dictionary<PropertyPath, ChildrenDefinition> _childrenDefinitions = new();
    protected readonly Dictionary<EventType, JoinDefinition> _joinDefinitions = new();
    protected AllDefinition _allDefinition = new(new Dictionary<PropertyPath, string>(), false);
    protected JsonObject _initialValues = (JsonObject)JsonNode.Parse("{}")!;
    protected EventType? _removedWithEvent;
    protected string _modelName;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionBuilder{TModel, TBuilder}"/> class.
    /// </summary>
    /// <param name="eventTypes"><see cref="IEventTypes"/> for providing event type information.</param>
    /// <param name="schemaGenerator"><see cref="IJsonSchemaGenerator"/> for generating JSON schemas.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for any JSON serialization.</param>
    public ProjectionBuilder(
        IEventTypes eventTypes,
        IJsonSchemaGenerator schemaGenerator,
        JsonSerializerOptions jsonSerializerOptions)
    {
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
    public TBuilder WithInitialValues(Func<TModel> initialValueProviderCallback)
    {
        var instance = initialValueProviderCallback();
        _initialValues = JsonObject.Create(JsonSerializer.SerializeToDocument(instance, typeof(TModel), _jsonSerializerOptions).RootElement)!;
        return (this as TBuilder)!;
    }

    /// <inheritdoc/>
    public TBuilder From<TEvent>(Action<IFromBuilder<TModel, TEvent>> builderCallback)
    {
        var builder = new FromBuilder<TModel, TEvent, TBuilder>(this);
        builderCallback(builder);
        var eventType = _eventTypes.GetEventTypeFor(typeof(TEvent));
        _fromDefinitions[eventType] = builder.Build();
        return (this as TBuilder)!;
    }

    /// <inheritdoc/>
    public TBuilder Join<TEvent>(Action<IJoinBuilder<TModel, TEvent>> builderCallback)
    {
        var builder = new JoinBuilder<TModel, TEvent, TBuilder>(this);
        builderCallback(builder);
        var eventType = _eventTypes.GetEventTypeFor(typeof(TEvent));
        _joinDefinitions[eventType] = builder.Build();
        return (this as TBuilder)!;
    }

    /// <inheritdoc/>
    public TBuilder FromEvery(Action<IFromEveryBuilder<TModel>> builderCallback)
    {
        var builder = new FromEveryBuilder<TModel>();
        builderCallback(builder);
        var allDefinition = builder.Build();
        _allDefinition = new AllDefinition(
            new Dictionary<PropertyPath, string>(_allDefinition.Properties.Concat(allDefinition.Properties)),
            allDefinition.IncludeChildren);
        return (this as TBuilder)!;
    }

    /// <inheritdoc/>
    public TBuilder RemovedWith<TEvent>()
    {
        if (_removedWithEvent != default)
        {
            throw new RemovalAlreadyDefined(GetType());
        }

        _removedWithEvent = _eventTypes.GetEventTypeFor(typeof(TEvent));
        return (this as TBuilder)!;
    }

    /// <inheritdoc/>
    public TBuilder Children<TChildModel>(Expression<Func<TModel, IEnumerable<TChildModel>>> targetProperty, Action<IChildrenBuilder<TModel, TChildModel>> builderCallback)
    {
        var builder = new ChildrenBuilder<TModel, TChildModel>(_eventTypes, _schemaGenerator, _jsonSerializerOptions);
        builderCallback(builder);
        _childrenDefinitions[targetProperty.GetPropertyPath()] = builder.Build();
        return (this as TBuilder)!;
    }
}
