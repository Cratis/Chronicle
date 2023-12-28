// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Contracts.Projections;
using Aksio.Cratis.Models;
using Aksio.Cratis.Projections.Definitions;
using Aksio.Cratis.Properties;
using Aksio.Cratis.Reflection;
using Aksio.Cratis.Schemas;
using Aksio.Reflection;
using Aksio.Strings;
using Humanizer;
using EventType = Aksio.Cratis.Kernel.Contracts.Events.EventType;

namespace Aksio.Cratis.Projections;

/// <summary>
/// Represents a base projection builder.
/// </summary>
/// <typeparam name="TModel">Type of model to build for.</typeparam>
/// <typeparam name="TBuilder">Type of actual builder.</typeparam>
public class ProjectionBuilder<TModel, TBuilder> : IProjectionBuilder<TModel, TBuilder>
    where TBuilder : class
{
#pragma warning disable CA1051 // Visible instance fields
#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable SA1629, CA1002, MA0016 // Return abstract
    protected readonly IEventTypes _eventTypes;
    protected readonly IJsonSchemaGenerator _schemaGenerator;
    protected readonly JsonSerializerOptions _jsonSerializerOptions;
    protected readonly Dictionary<EventType, FromDefinition> _fromDefinitions = new();
    protected readonly Dictionary<PropertyPath, ChildrenDefinition> _childrenDefinitions = new();
    protected readonly Dictionary<EventType, JoinDefinition> _joinDefinitions = new();
    protected readonly List<FromAnyDefinition> _fromAnyDefinitions = new();
    protected AllDefinition _allDefinition = new(new Dictionary<PropertyPath, string>(), false);
    protected JsonObject _initialValues = (JsonObject)JsonNode.Parse("{}")!;
    protected EventType? _removedWithEvent;
    protected string _modelName;
#pragma warning restore CA1629, CA1002, MA0016 // Return abstract
#pragma warning restore CA1600 // Elements should be documented
#pragma warning restore CA1051 // Visible instance fields

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
        var type = typeof(TEvent);

        if (!type.IsEventType(_eventTypes.AllClrTypes))
        {
            throw new TypeIsNotAnEventType(typeof(TEvent));
        }

        var eventTypes = type.GetEventTypes(_eventTypes.AllClrTypes).Select(_eventTypes.GetEventTypeFor).ToArray();

        var builder = new FromBuilder<TModel, TEvent, TBuilder>(this);
        builderCallback(builder);
        var fromDefinition = builder.Build();

        if (eventTypes.Length > 1)
        {
            _fromAnyDefinitions.Add(new FromAnyDefinition(eventTypes, fromDefinition));
        }
        else
        {
            _fromDefinitions[eventTypes[0]] = fromDefinition;
        }
        return (this as TBuilder)!;
    }

    /// <inheritdoc/>
    public TBuilder Join<TEvent>(Action<IJoinBuilder<TModel, TEvent>> builderCallback)
    {
        if (!typeof(TEvent).IsEventType(_eventTypes.AllClrTypes))
        {
            throw new TypeIsNotAnEventType(typeof(TEvent));
        }

        var builder = new JoinBuilder<TModel, TEvent, TBuilder>(this);
        builderCallback(builder);
        var eventType = _eventTypes.GetEventTypeFor(typeof(TEvent));
        _joinDefinitions[eventType.ToContract()] = builder.Build();
        return (this as TBuilder)!;
    }

    /// <inheritdoc/>
    public TBuilder FromEvery(Action<IFromEveryBuilder<TModel>> builderCallback)
    {
        var builder = new FromEveryBuilder<TModel>();
        builderCallback(builder);
        var allDefinition = builder.Build();
        _allDefinition = new AllDefinition
        {
            Properties = new Dictionary<string, string>(_allDefinition.Properties.Concat(allDefinition.Properties)),
            IncludeChildren = allDefinition.IncludeChildren
        };
        return (this as TBuilder)!;
    }

    /// <inheritdoc/>
    public TBuilder RemovedWith<TEvent>()
    {
        if (_removedWithEvent != default)
        {
            throw new RemovalAlreadyDefined(GetType());
        }

        _removedWithEvent = _eventTypes.GetEventTypeFor(typeof(TEvent)).ToContract();
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
