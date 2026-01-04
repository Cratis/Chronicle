// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.ReadModels;
using Cratis.Serialization;
using EventType = Cratis.Chronicle.Contracts.Events.EventType;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents a base projection builder.
/// </summary>
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> to use for converting names during serialization.</param>
/// <param name="eventTypes"><see cref="IEventTypes"/> for providing event type information.</param>
/// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for any JSON serialization.</param>
/// <param name="autoMap">Whether to automatically map properties.</param>
/// <typeparam name="TReadModel">Type of read model to build for.</typeparam>
/// <typeparam name="TBuilder">Type of actual builder.</typeparam>
public class ProjectionBuilder<TReadModel, TBuilder>(
    INamingPolicy namingPolicy,
    IEventTypes eventTypes,
    JsonSerializerOptions jsonSerializerOptions,
    bool autoMap) : IProjectionBuilder<TReadModel, TBuilder>
    where TBuilder : class
{
#pragma warning disable CA1051 // Visible instance fields
#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable SA1629, CA1002, MA0016 // Return abstract
    protected readonly Dictionary<EventType, FromDefinition> _fromDefinitions = [];
    protected readonly Dictionary<PropertyPath, ChildrenDefinition> _childrenDefinitions = [];
    protected readonly Dictionary<EventType, JoinDefinition> _joinDefinitions = [];
    protected readonly List<FromDerivativesDefinition> _fromDerivativesDefinitions = [];
    protected readonly Dictionary<EventType, RemovedWithDefinition> _removedWithDefinitions = [];
    protected readonly Dictionary<EventType, RemovedWithJoinDefinition> _removedWithJoinDefinitions = [];
    protected FromEveryDefinition _fromEveryDefinition = new();
    protected JsonObject _initialValues = (JsonObject)JsonNode.Parse("{}")!;
    protected bool _autoMap = autoMap;
    protected ReadModelIdentifier _readModelIdentifier = typeof(TReadModel).GetReadModelIdentifier();

    /// <inheritdoc/>
    public TBuilder WithInitialValues(Func<TReadModel> initialValueProviderCallback)
    {
        var instance = initialValueProviderCallback();
        _initialValues = JsonObject.Create(JsonSerializer.SerializeToDocument(instance, jsonSerializerOptions).RootElement)!;
        return (this as TBuilder)!;
    }

    /// <inheritdoc/>
    public IProjectionBuilder<TReadModel, TBuilder> AutoMap()
    {
        _autoMap = true;
        return this;
    }

    /// <inheritdoc/>
    public IProjectionBuilder<TReadModel, TBuilder> NoAutoMap()
    {
        _autoMap = false;
        return this;
    }

    /// <inheritdoc/>
    public TBuilder From<TEvent>(Action<IFromBuilder<TReadModel, TEvent>>? builderCallback = default)
    {
        var type = typeof(TEvent);

        if (!type.IsEventType(eventTypes.AllClrTypes))
        {
            throw new TypeIsNotAnEventType(typeof(TEvent));
        }

        var eventTypesInProjection = type.GetEventTypes(eventTypes.AllClrTypes).Select(eventTypes.GetEventTypeFor).ToArray();

        var builder = new FromBuilder<TReadModel, TEvent, TBuilder>(this, namingPolicy);

        if (_autoMap)
        {
            builder.AutoMap();
        }

        builderCallback?.Invoke(builder);
        var fromDefinition = builder.Build();

        if (eventTypesInProjection.Length > 1)
        {
            _fromDerivativesDefinitions.Add(new FromDerivativesDefinition
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
    public TBuilder Join<TEvent>(Action<IJoinBuilder<TReadModel, TEvent>>? builderCallback = default)
    {
        if (!typeof(TEvent).IsEventType(eventTypes.AllClrTypes))
        {
            throw new TypeIsNotAnEventType(typeof(TEvent));
        }

        var builder = new JoinBuilder<TReadModel, TEvent, TBuilder>(this, namingPolicy);

        if (_autoMap)
        {
            builder.AutoMap();
        }

        builderCallback?.Invoke(builder);
        var eventType = eventTypes.GetEventTypeFor(typeof(TEvent));
        _joinDefinitions[eventType.ToContract()] = builder.Build();
        return (this as TBuilder)!;
    }

    /// <inheritdoc/>
    public TBuilder FromEvery(Action<IFromEveryBuilder<TReadModel>> builderCallback)
    {
        var builder = new FromEveryBuilder<TReadModel>(namingPolicy);
        builderCallback(builder);
        var fromEveryDefinition = builder.Build();
        _fromEveryDefinition = new FromEveryDefinition
        {
            Properties = new Dictionary<string, string>(_fromEveryDefinition.Properties.Concat(fromEveryDefinition.Properties)),
            IncludeChildren = fromEveryDefinition.IncludeChildren
        };
        return (this as TBuilder)!;
    }

    /// <inheritdoc/>
    public TBuilder RemovedWith<TEvent>(Action<RemovedWithBuilder<TReadModel, TEvent>>? builderCallback = default)
    {
        var type = typeof(TEvent);

        if (!type.IsEventType(eventTypes.AllClrTypes))
        {
            throw new TypeIsNotAnEventType(typeof(TEvent));
        }

        var removedWithEvent = eventTypes.GetEventTypeFor(typeof(TEvent)).ToContract();
        var removedWithBuilder = new RemovedWithBuilder<TReadModel, TEvent>(namingPolicy);
        builderCallback?.Invoke(removedWithBuilder);
        _removedWithDefinitions[removedWithEvent] = removedWithBuilder.Build();

        return (this as TBuilder)!;
    }

    /// <inheritdoc/>
    public TBuilder RemovedWithJoin<TEvent>(Action<RemovedWithJoinBuilder<TReadModel, TEvent>>? builderCallback = default)
    {
        var type = typeof(TEvent);

        if (!type.IsEventType(eventTypes.AllClrTypes))
        {
            throw new TypeIsNotAnEventType(typeof(TEvent));
        }

        var removedWithJoinEvent = eventTypes.GetEventTypeFor(typeof(TEvent)).ToContract();
        var removedWithJoinBuilder = new RemovedWithJoinBuilder<TReadModel, TEvent>(namingPolicy);
        builderCallback?.Invoke(removedWithJoinBuilder);
        _removedWithJoinDefinitions[removedWithJoinEvent] = removedWithJoinBuilder.Build();

        return (this as TBuilder)!;
    }

    /// <inheritdoc/>
    public TBuilder Children<TChildModel>(Expression<Func<TReadModel, IEnumerable<TChildModel>>> targetProperty, Action<IChildrenBuilder<TReadModel, TChildModel>> builderCallback)
    {
        var builder = new ChildrenBuilder<TReadModel, TChildModel>(namingPolicy, eventTypes, jsonSerializerOptions, _autoMap);
        builderCallback(builder);
        _childrenDefinitions[namingPolicy.GetPropertyName(targetProperty.GetPropertyPath())] = builder.Build();
        return (this as TBuilder)!;
    }
}
