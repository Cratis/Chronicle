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
            [eventType.Id],
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
        var constraints = MergeUniqueEventTypeConstraintsSharingName(_constraints);
        ThrowIfDuplicateConstraintNames(constraints);

        return constraints.ToImmutableList();
    }

    /// <summary>
    /// Merge unique event type constraints declared under the same name into a single definition.
    /// </summary>
    /// <param name="constraints">The constraints to merge.</param>
    /// <returns>The constraints with same-named unique event type definitions merged.</returns>
    /// <remarks>
    /// Declaring <c>Unique&lt;A&gt;(name: x)</c> and <c>Unique&lt;B&gt;(name: x)</c> is how mutual exclusion is
    /// expressed — the two become one constraint allowing at most one event from {A, B} per event source.
    /// Merging happens here rather than downstream so that names stay unique across the built set, which
    /// registration, change detection, and violation message resolution all rely on.
    /// </remarks>
    static List<IConstraintDefinition> MergeUniqueEventTypeConstraintsSharingName(IEnumerable<IConstraintDefinition> constraints)
    {
        var merged = new List<IConstraintDefinition>();
        foreach (var constraint in constraints)
        {
            if (constraint is not UniqueEventTypeConstraintDefinition uniqueEventType)
            {
                merged.Add(constraint);
                continue;
            }

            var existingIndex = merged.FindIndex(_ => _ is UniqueEventTypeConstraintDefinition && _.Name == constraint.Name);
            if (existingIndex < 0)
            {
                merged.Add(uniqueEventType);
                continue;
            }

            var existing = (UniqueEventTypeConstraintDefinition)merged[existingIndex];
            merged[existingIndex] = existing with
            {
                EventTypeIds = existing.EventTypeIds.Concat(uniqueEventType.EventTypeIds).Distinct().ToArray()
            };
        }

        return merged;
    }

    static void ThrowIfDuplicateConstraintNames(IEnumerable<IConstraintDefinition> constraints)
    {
        var violatingConstraints = constraints
            .GroupBy(_ => _.Name)
            .Where(_ => _.Count() > 1)
            .Select(_ => _.Key)
            .ToArray();

        if (violatingConstraints.Length > 0)
        {
            throw new DuplicateConstraintNames(violatingConstraints);
        }
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
}
