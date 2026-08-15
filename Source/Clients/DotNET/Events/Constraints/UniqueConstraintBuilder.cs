// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Serialization;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Represents an implementation of <see cref="IUniqueConstraintBuilder"/>.
/// </summary>
/// <param name="eventTypes">Event types for the builder.</param>
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> to use for converting names during serialization.</param>
/// <param name="owner">Optional owner of the constraint.</param>
public class UniqueConstraintBuilder(
    IEventTypes eventTypes,
    INamingPolicy namingPolicy,
    Type? owner = default) : IUniqueConstraintBuilder
{
    readonly List<UniqueConstraintEventDefinition> _eventTypesAndProperties = [];
    readonly Dictionary<EventTypeId, JsonSchema> _eventTypeSchemas = [];
    readonly List<EventTypeId> _removedWith = [];
    ConstraintName? _name;
    ConstraintViolationMessageProvider? _messageProvider;
    bool _ignoreCasing;

    /// <inheritdoc/>
    public IUniqueConstraintBuilder On<TEventType>(params Expression<Func<TEventType, object>>[] properties)
    {
        var eventType = eventTypes.GetEventTypeFor(typeof(TEventType));
        var propertiesAsStrings = properties.Select(_ => _.GetPropertyPath().Path).ToArray();
        return On(eventType, propertiesAsStrings);
    }

    /// <inheritdoc/>
    public IUniqueConstraintBuilder On(EventType eventType, params string[] properties)
    {
        properties = properties.Select(_ => namingPolicy.GetPropertyName(new PropertyPath(_))).ToArray();
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
    /// <remarks>
    /// Collected rather than replaced. A lifecycle can end in more than one way — an invitation is released by
    /// being accepted, revoked or expiring — and each declaration used to overwrite the previous one, so every
    /// terminal event but the last compiled, registered and released nothing.
    /// </remarks>
    public IUniqueConstraintBuilder RemovedWith(EventType eventType)
    {
        if (!_removedWith.Contains(eventType.Id))
        {
            _removedWith.Add(eventType.Id);
        }

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
            [.. _removedWith],
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
        foreach (var property in properties)
        {
            if (schema.GetSchemaPropertyForPropertyPath(new PropertyPath(property)) is null)
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
