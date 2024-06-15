// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Models;
using Cratis.Reflection;
using Cratis.Strings;
using Humanizer;
using EventType = Cratis.Chronicle.Contracts.Events.EventType;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents a base projection builder.
/// </summary>
/// <typeparam name="TModel">Type of model to build for.</typeparam>
/// <typeparam name="TBuilder">Type of actual builder.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="ProjectionBuilder{TModel, TBuilder}"/> class.
/// </remarks>
/// <param name="eventTypes"><see cref="IEventTypes"/> for providing event type information.</param>
/// <param name="schemaGenerator"><see cref="IJsonSchemaGenerator"/> for generating JSON schemas.</param>
/// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for any JSON serialization.</param>
public class ProjectionBuilder<TModel, TBuilder>(
    IEventTypes eventTypes,
    IJsonSchemaGenerator schemaGenerator,
    JsonSerializerOptions jsonSerializerOptions) : IProjectionBuilder<TModel, TBuilder>
    where TBuilder : class
{
#pragma warning disable CA1051 // Visible instance fields
#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable SA1629, CA1002, MA0016 // Return abstract
    protected readonly Dictionary<EventType, FromDefinition> _fromDefinitions = [];
    protected readonly Dictionary<PropertyPath, ChildrenDefinition> _childrenDefinitions = [];
    protected readonly Dictionary<EventType, JoinDefinition> _joinDefinitions = [];
    protected readonly List<FromAnyDefinition> _fromAnyDefinitions = [];
    protected AllDefinition _allDefinition = new();
    protected JsonObject _initialValues = (JsonObject)JsonNode.Parse("{}")!;
    protected EventType? _removedWithEvent;
    protected string _modelName = typeof(TModel).HasAttribute<ModelNameAttribute>() ? typeof(TModel).GetCustomAttribute<ModelNameAttribute>()!.Name : typeof(TModel).Name.Pluralize().ToCamelCase();

    /// <inheritdoc/>
    public TBuilder WithInitialValues(Func<TModel> initialValueProviderCallback)
    {
        var instance = initialValueProviderCallback();
        _initialValues = JsonObject.Create(JsonSerializer.SerializeToDocument(instance, typeof(TModel), jsonSerializerOptions).RootElement)!;
        return (this as TBuilder)!;
    }

    /// <inheritdoc/>
    public TBuilder From<TEvent>(Action<IFromBuilder<TModel, TEvent>> builderCallback)
    {
        var type = typeof(TEvent);

        if (!type.IsEventType(eventTypes.AllClrTypes))
        {
            throw new TypeIsNotAnEventType(typeof(TEvent));
        }

        var eventTypesInProjection = type.GetEventTypes(eventTypes.AllClrTypes).Select(eventTypes.GetEventTypeFor).ToArray();

        var builder = new FromBuilder<TModel, TEvent, TBuilder>(this);
        builderCallback(builder);
        var fromDefinition = builder.Build();

        if (eventTypesInProjection.Length > 1)
        {
            _fromAnyDefinitions.Add(new FromAnyDefinition
            {
                EventTypes = eventTypesInProjection.ToContract(),
                From = fromDefinition
            });
        }
        else
        {
            _fromDefinitions[eventTypesInProjection[0].ToContract()] = fromDefinition;
        }
        return (this as TBuilder)!;
    }

    /// <inheritdoc/>
    public TBuilder Join<TEvent>(Action<IJoinBuilder<TModel, TEvent>> builderCallback)
    {
        if (!typeof(TEvent).IsEventType(eventTypes.AllClrTypes))
        {
            throw new TypeIsNotAnEventType(typeof(TEvent));
        }

        var builder = new JoinBuilder<TModel, TEvent, TBuilder>(this);
        builderCallback(builder);
        var eventType = eventTypes.GetEventTypeFor(typeof(TEvent));
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

        _removedWithEvent = eventTypes.GetEventTypeFor(typeof(TEvent)).ToContract();
        return (this as TBuilder)!;
    }

    /// <inheritdoc/>
    public TBuilder Children<TChildModel>(Expression<Func<TModel, IEnumerable<TChildModel>>> targetProperty, Action<IChildrenBuilder<TModel, TChildModel>> builderCallback)
    {
        var builder = new ChildrenBuilder<TModel, TChildModel>(eventTypes, schemaGenerator, jsonSerializerOptions);
        builderCallback(builder);
        _childrenDefinitions[targetProperty.GetPropertyPath()] = builder.Build();
        return (this as TBuilder)!;
    }
}
