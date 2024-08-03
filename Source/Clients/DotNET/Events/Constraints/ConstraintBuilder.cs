// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Represents an implementation of <see cref="IConstraintBuilder"/>.
/// </summary>
/// <param name="eventTypes">Event types for the builder.</param>
/// <param name="owner">Optional owner of the constraint.</param>
public class ConstraintBuilder(IEventTypes eventTypes, Type? owner = default) : IConstraintBuilder
{
    readonly List<IConstraintDefinition> _constraints = [];

    /// <inheritdoc/>
    public IConstraintBuilder Unique(Action<IUniqueConstraintBuilder> callback)
    {
        var uniqueConstraintBuilder = new UniqueConstraintBuilder(eventTypes, owner);
        callback(uniqueConstraintBuilder);
        AddConstraint(uniqueConstraintBuilder.Build());
        return this;
    }

    /// <inheritdoc/>
    public IConstraintBuilder Unique<TEventType>(ConstraintViolationMessage? message = default, ConstraintName? name = default)
    {
        return Unique<TEventType>(
            eventType => message ?? $"An instance of event type {eventType.Id} already exists",
            name);
    }

    /// <inheritdoc/>
    public IConstraintBuilder Unique<TEventType>(Func<EventType, ConstraintViolationMessage> messageCallback, ConstraintName? name = default)
    {
        var eventType = eventTypes.GetEventTypeFor(typeof(TEventType));
        AddConstraint(new UniqueEventTypeConstraintDefinition(
            name ?? eventType.Id.Value,
            messageCallback,
            eventType));

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
