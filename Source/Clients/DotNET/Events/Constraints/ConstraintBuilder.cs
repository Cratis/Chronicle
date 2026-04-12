// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Serialization;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Represents an implementation of <see cref="IConstraintBuilder"/>.
/// </summary>
/// <param name="eventTypes">Event types for the builder.</param>
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> to use for converting names during serialization.</param>
/// <param name="owner">Optional owner of the constraint.</param>
public class ConstraintBuilder(
    IEventTypes eventTypes,
    INamingPolicy namingPolicy,
    Type? owner = default) : IConstraintBuilder
{
    readonly List<IConstraintDefinition> _constraints = [];
    bool _perEventSourceType;
    bool _perEventStreamType;
    bool _perEventStreamId;

    /// <inheritdoc/>
    public IConstraintBuilder PerEventSourceType()
    {
        _perEventSourceType = true;
        return this;
    }

    /// <inheritdoc/>
    public IConstraintBuilder PerEventStreamType()
    {
        _perEventStreamType = true;
        return this;
    }

    /// <inheritdoc/>
    public IConstraintBuilder PerEventStreamId()
    {
        _perEventStreamId = true;
        return this;
    }

    /// <inheritdoc/>
    public IConstraintBuilder Unique(Action<IUniqueConstraintBuilder> callback)
    {
        var uniqueConstraintBuilder = new UniqueConstraintBuilder(eventTypes, namingPolicy, owner);
        callback(uniqueConstraintBuilder);
        var definition = uniqueConstraintBuilder.Build();
        AddConstraint(ApplyScope(definition));
        return this;
    }

    /// <inheritdoc/>
    public IConstraintBuilder Unique<TEventType>(ConstraintViolationMessage? message = default, ConstraintName? name = default)
    {
        return Unique<TEventType>(
            eventType => message ?? string.Empty,
            name);
    }

    /// <inheritdoc/>
    public IConstraintBuilder Unique<TEventType>(ConstraintViolationMessageProvider messageCallback, ConstraintName? name = default)
    {
        var eventType = eventTypes.GetEventTypeFor(typeof(TEventType));
        AddConstraint(ApplyScope(new UniqueEventTypeConstraintDefinition(
            name ?? eventType.Id.Value,
            messageCallback,
            eventType.Id,
            null)));

        return this;
    }

    /// <inheritdoc/>
    public void AddConstraint(IConstraintDefinition constraint)
    {
        _constraints.Add(constraint);
    }

    /// <inheritdoc/>
    public IImmutableList<IConstraintDefinition> Build()
    {
        ThrowIfDuplicateConstraintNames();

        return _constraints.ToImmutableList();
    }

    ConstraintScope? GetScope()
    {
        if (!_perEventSourceType && !_perEventStreamType && !_perEventStreamId)
        {
            return null;
        }

        return new ConstraintScope(
            _perEventSourceType ? (EventSourceType)"_scoped_" : null,
            _perEventStreamType ? (EventStreamType)"_scoped_" : null,
            _perEventStreamId ? (EventStreamId)"_scoped_" : null);
    }

    IConstraintDefinition ApplyScope(IConstraintDefinition definition)
    {
        var scope = GetScope();
        if (scope is null)
        {
            return definition;
        }

        return definition switch
        {
            UniqueConstraintDefinition unique => unique with { Scope = scope },
            UniqueEventTypeConstraintDefinition uniqueEventType => uniqueEventType with { Scope = scope },
            _ => definition
        };
    }

    void ThrowIfDuplicateConstraintNames()
    {
        var violatingConstraints = _constraints
            .GroupBy(_ => _.Name)
            .Where(_ => _.Count() > 1)
            .Select(_ => _.Key)
            .ToArray();

        if (violatingConstraints.Length > 0)
        {
            throw new DuplicateConstraintNames(violatingConstraints);
        }
    }
}
