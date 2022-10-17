// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aksio.Cratis.Events.Projections.Definitions;
using Aksio.Cratis.Properties;
using Aksio.Cratis.Reflection;
using Aksio.Cratis.Schemas;
using Aksio.Cratis.Strings;
using Humanizer;

namespace Aksio.Cratis.Events.Projections;

/// <summary>
/// Represents an implementation of <see cref="IChildrenBuilder{TModel, TChildModel}"/>.
/// </summary>
/// <typeparam name="TParentModel">Parent model type.</typeparam>
/// <typeparam name="TChildModel">Child model type.</typeparam>
public class ChildrenBuilder<TParentModel, TChildModel> : IChildrenBuilder<TParentModel, TChildModel>
{
    readonly IEventTypes _eventTypes;
    readonly IJsonSchemaGenerator _schemaGenerator;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    readonly Dictionary<EventType, FromDefinition> _fromDefinitions = new();
    readonly Dictionary<EventType, JoinDefinition> _joinDefinitions = new();
    readonly Dictionary<PropertyPath, ChildrenDefinition> _childrenDefinitions = new();
    readonly string _modelName;
    string _identifiedBy = string.Empty;
    EventType? _removedWithEvent;
    JsonObject _initialValues = (JsonObject)JsonNode.Parse("{}")!;

    /// <summary>
    /// /// Initializes a new instance of the <see cref="ProjectionBuilderFor{TModel}"/> class.
    /// </summary>
    /// <param name="eventTypes"><see cref="IEventTypes"/> for providing event type information.</param>
    /// <param name="schemaGenerator"><see cref="IJsonSchemaGenerator"/> for generating JSON schemas.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for any JSON serialization.</param>
    public ChildrenBuilder(
        IEventTypes eventTypes,
        IJsonSchemaGenerator schemaGenerator,
        JsonSerializerOptions jsonSerializerOptions)
    {
        _eventTypes = eventTypes;
        _schemaGenerator = schemaGenerator;
        _jsonSerializerOptions = jsonSerializerOptions;
        _modelName = typeof(TChildModel).Name.Pluralize().ToCamelCase();
    }

    /// <inheritdoc/>
    public IChildrenBuilder<TParentModel, TChildModel> WithInitialValues(Func<TChildModel> initialValueProviderCallback)
    {
        var instance = initialValueProviderCallback();
        _initialValues = JsonObject.Create(JsonSerializer.SerializeToDocument(instance, typeof(TChildModel), _jsonSerializerOptions).RootElement)!;
        return this;
    }

    /// <inheritdoc/>
    public IChildrenBuilder<TParentModel, TChildModel> From<TEvent>(Action<IFromBuilder<TChildModel, TEvent>> builderCallback)
    {
        var builder = new FromBuilder<TChildModel, TEvent>();
        builderCallback(builder);
        var eventType = _eventTypes.GetEventTypeFor(typeof(TEvent));
        _fromDefinitions[eventType] = builder.Build();
        return this;
    }

    /// <inheritdoc/>
    public IChildrenBuilder<TParentModel, TChildModel> Join<TEvent>(Action<IJoinBuilder<TChildModel, TEvent>> builderCallback)
    {
        var builder = new JoinBuilder<TChildModel, TEvent>();
        builderCallback(builder);
        var eventType = _eventTypes.GetEventTypeFor(typeof(TEvent));
        _joinDefinitions[eventType] = builder.Build();
        return this;
    }

    /// <inheritdoc/>
    public IChildrenBuilder<TParentModel, TChildModel> IdentifiedBy<TProperty>(Expression<Func<TChildModel, TProperty>> propertyExpression)
    {
        _identifiedBy = propertyExpression.GetPropertyPath();
        return this;
    }

    /// <inheritdoc/>
    public IChildrenBuilder<TParentModel, TChildModel> RemovedWith<TEvent>()
    {
        _removedWithEvent = _eventTypes.GetEventTypeFor(typeof(TEvent));
        return this;
    }

    /// <inheritdoc/>
    public IChildrenBuilder<TParentModel, TChildModel> Children<TNestedChildModel>(Expression<Func<TChildModel, IEnumerable<TNestedChildModel>>> targetProperty, Action<IChildrenBuilder<TChildModel, TNestedChildModel>> builderCallback)
    {
        var builder = new ChildrenBuilder<TChildModel, TNestedChildModel>(_eventTypes, _schemaGenerator, _jsonSerializerOptions);
        builderCallback(builder);
        _childrenDefinitions[targetProperty.GetPropertyPath()] = builder.Build();
        return this;
    }

    /// <inheritdoc/>
    public ChildrenDefinition Build()
    {
        return new ChildrenDefinition(
            _identifiedBy,
            new ModelDefinition(_modelName, _schemaGenerator.Generate(typeof(TChildModel)).ToJson()),
            _initialValues,
            _fromDefinitions,
            _joinDefinitions,
            _childrenDefinitions,
            _removedWithEvent == default ? default : new RemovedWithDefinition(_removedWithEvent));
    }
}
