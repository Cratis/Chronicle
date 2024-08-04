// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Chronicle.Schemas;
using Cratis.Reflection;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Represents an implementation of <see cref="IUniqueConstraintBuilder"/>.
/// </summary>
/// <param name="eventTypes">Event types for the builder.</param>
/// <param name="owner">Optional owner of the constraint.</param>
public class UniqueConstraintBuilder(IEventTypes eventTypes, Type? owner = default) : IUniqueConstraintBuilder
{
    readonly List<EventTypeAndProperty> _eventTypesAndProperties = [];
    ConstraintName? _name;
    ConstraintViolationMessageProvider? _messageProvider;

    /// <inheritdoc/>
    public IUniqueConstraintBuilder On<TEventType>(Expression<Func<TEventType, object>> property)
    {
        var eventType = eventTypes.GetEventTypeFor(typeof(TEventType));
        return On(eventType, property.GetPropertyInfo().Name);
    }

    /// <inheritdoc/>
    public IUniqueConstraintBuilder On(EventType eventType, string property)
    {
        ThrowIfEventTypeAlreadyAdded(eventType, property);
        ThrowIfPropertyIsMissing(eventType, property);
        ThrowIfPropertyTypeMismatch(eventType, property);

        _eventTypesAndProperties.Add(new EventTypeAndProperty(eventType, eventTypes.GetSchemaFor(eventType.Id), property));
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
    public IConstraintDefinition Build()
    {
        ThrowIfNoEventTypesAdded();

        var name = _name ?? owner?.Name ?? throw new MissingNameForUniqueConstraint();

        ConstraintViolationMessageProvider defaultMessageProvider = _ => string.Empty;
        var messageProvider = _messageProvider ?? defaultMessageProvider;

        return new UniqueConstraintDefinition(name, messageProvider, [.. _eventTypesAndProperties]);
    }

    void ThrowIfNoEventTypesAdded()
    {
        if (_eventTypesAndProperties.Count == 0)
        {
            throw new NoEventTypesAddedToUniqueConstraint();
        }
    }

    void ThrowIfPropertyIsMissing(EventType eventType, string property)
    {
        var schema = eventTypes.GetSchemaFor(eventType.Id);
        if (!schema.GetFlattenedProperties().Any(_ => _.Name == property))
        {
            throw new PropertyDoesNotExistOnEventType(eventType, property);
        }
    }

    void ThrowIfEventTypeAlreadyAdded(EventType eventType, string property)
    {
        if (_eventTypesAndProperties.Exists(_ => _.EventType == eventType))
        {
            throw new EventTypeAlreadyAddedToUniqueConstraint(string.Empty, eventType, property);
        }
    }

    void ThrowIfPropertyTypeMismatch(EventType eventType, string property)
    {
        var schema = eventTypes.GetSchemaFor(eventType.Id);
        var propertySchema = schema.GetFlattenedProperties().First(p => p.Name == property);
        if (_eventTypesAndProperties.Exists(_ => _.Schema.GetFlattenedProperties().First(p => p.Name == _.Property).Type != propertySchema.Type))
        {
            throw new PropertyTypeMismatchInUniqueConstraint(eventType, property);
        }
    }
}
