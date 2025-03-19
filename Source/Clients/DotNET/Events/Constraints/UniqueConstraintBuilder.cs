// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Chronicle.Schemas;
using Cratis.Reflection;
using Cratis.Strings;
using NJsonSchema;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Represents an implementation of <see cref="IUniqueConstraintBuilder"/>.
/// </summary>
/// <param name="eventTypes">Event types for the builder.</param>
/// <param name="owner">Optional owner of the constraint.</param>
public class UniqueConstraintBuilder(IEventTypes eventTypes, Type? owner = default) : IUniqueConstraintBuilder
{
    readonly List<UniqueConstraintEventDefinition> _eventTypesAndProperties = [];
    readonly Dictionary<EventTypeId, JsonSchema> _eventTypeSchemas = [];
    ConstraintName? _name;
    ConstraintViolationMessageProvider? _messageProvider;
    EventTypeId? _removedWith;
    bool _ignoreCasing;

    /// <inheritdoc/>
    public IUniqueConstraintBuilder On<TEventType>(params Expression<Func<TEventType, object>>[] properties)
    {
        var eventType = eventTypes.GetEventTypeFor(typeof(TEventType));
        var propertiesAsStrings = properties.Select(_ => _.GetPropertyInfo().Name).ToArray();
        return On(eventType, propertiesAsStrings);
    }

    /// <inheritdoc/>
    public IUniqueConstraintBuilder On(EventType eventType, params string[] properties)
    {
        properties = properties.Select(_ => _.ToCamelCase()).ToArray();
        var schema = eventTypes.GetSchemaFor(eventType.Id);
        ThrowIfEventTypeAlreadyAdded(eventType, properties);
        ThrowIfPropertyIsMissing(eventType, schema, properties);

        _eventTypesAndProperties.Add(new UniqueConstraintEventDefinition(eventType.Id, properties));
        _eventTypeSchemas[eventType.Id] = schema;
        return this;
    }

    /// <inheritdoc/>
    public IUniqueConstraintBuilder IgnoreCasing()
    {
        _ignoreCasing = true;
        return this;
    }

    /// <inheritdoc/>
    public IUniqueConstraintBuilder WithMessage(string message) => WithMessage(_ => message);

    /// <inheritdoc/>
    public IUniqueConstraintBuilder WithMessage(ConstraintViolationMessageProvider messageProvider)
    {
        _messageProvider = messageProvider;
        return this;
    }

    /// <inheritdoc/>
    public IUniqueConstraintBuilder WithName(ConstraintName name)
    {
        _name = name;
        return this;
    }

    /// <inheritdoc/>
    public IUniqueConstraintBuilder RemovedWith<TEventType>() =>
        RemovedWith(eventTypes.GetEventTypeFor(typeof(TEventType)));

    /// <inheritdoc/>
    public IUniqueConstraintBuilder RemovedWith(EventType eventType)
    {
        _removedWith = eventType.Id;
        return this;
    }

    /// <inheritdoc/>
    public IConstraintDefinition Build()
    {
        ThrowIfNoEventTypesAdded();

        var name = _name ?? owner?.Name ?? throw new MissingNameForUniqueConstraint();

        ConstraintViolationMessageProvider defaultMessageProvider = _ => string.Empty;
        var messageProvider = _messageProvider ?? defaultMessageProvider;

        return new UniqueConstraintDefinition(
            name,
            messageProvider,
            [.. _eventTypesAndProperties],
            _removedWith,
            _ignoreCasing);
    }

    void ThrowIfNoEventTypesAdded()
    {
        if (_eventTypesAndProperties.Count == 0)
        {
            throw new NoEventTypesAddedToUniqueConstraint();
        }
    }

    void ThrowIfPropertyIsMissing(EventType eventType, JsonSchema schema, IEnumerable<string> properties)
    {
        var schemaProperties = schema.GetFlattenedProperties();
        foreach (var property in properties)
        {
            if (!schemaProperties.Any(_ => _.Name == property))
            {
                throw new PropertyDoesNotExistOnEventType(eventType, property);
            }
        }
    }

    void ThrowIfEventTypeAlreadyAdded(EventType eventType, IEnumerable<string> properties)
    {
        if (_eventTypesAndProperties.Exists(_ => _.EventTypeId == eventType.Id))
        {
            throw new EventTypeAlreadyAddedToUniqueConstraint(string.Empty, eventType, properties);
        }
    }
}
