// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Properties;
using Cratis.Serialization;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Builds one closing-event declaration with an optional property-sourced stream identifier.
/// </summary>
/// <typeparam name="TEvent">The closing event type.</typeparam>
/// <param name="eventTypes">The event types and schemas.</param>
/// <param name="namingPolicy">The payload naming policy.</param>
public class ClosesStreamBuilder<TEvent>(IEventTypes eventTypes, INamingPolicy namingPolicy) : IClosesStreamBuilder<TEvent>
{
    readonly List<Type> _reopenedBy = [];
    ClosedStreamDimensions _dimensions;
    string? _property;

    /// <inheritdoc/>
    public IClosesStreamBuilder<TEvent> PerEventSourceId() => Include(ClosedStreamDimensions.EventSourceId);

    /// <inheritdoc/>
    public IClosesStreamBuilder<TEvent> PerEventSourceType() => Include(ClosedStreamDimensions.EventSourceType);

    /// <inheritdoc/>
    public IClosesStreamBuilder<TEvent> PerEventStreamType() => Include(ClosedStreamDimensions.EventStreamType);

    /// <inheritdoc/>
    public IClosesStreamBuilder<TEvent> PerEventStreamId() => Include(ClosedStreamDimensions.EventStreamId);

    /// <inheritdoc/>
    public IClosesStreamBuilder<TEvent> EventStreamIdFrom(Expression<Func<TEvent, object?>> property)
    {
        _property = namingPolicy.GetPropertyName(property.GetPropertyPath());
        return this;
    }

    /// <inheritdoc/>
    public IClosesStreamBuilder<TEvent> ReopenedBy<TReopen>()
    {
        if (!_reopenedBy.Contains(typeof(TReopen))) _reopenedBy.Add(typeof(TReopen));
        return this;
    }

    /// <summary>
    /// Build the declaration and verify its property on every closing and reopening event.
    /// </summary>
    /// <param name="name">The owner name.</param>
    /// <returns>The closing declaration.</returns>
    /// <exception cref="PropertyDoesNotExistOnEventType">A participating event lacks the property.</exception>
    /// <exception cref="MissingNameForClosesStreamConstraint">The name is reserved for manual closures.</exception>
    public ClosesStreamConstraintDefinition Build(ConstraintName name)
    {
        if (name.Value.Length == 0) throw new MissingNameForClosesStreamConstraint();
        ClosesStreamDeclarations.ValidateProperty(eventTypes, new[] { typeof(TEvent) }.Concat(_reopenedBy), _property);
        var dimensions = _dimensions == ClosedStreamDimensions.None ? ClosesStreamDeclarations.DefaultDimensions : _dimensions;
        if (_property is not null) dimensions |= ClosedStreamDimensions.EventStreamId;

        return new(name, _ => ConstraintViolationMessage.NotDefined, [eventTypes.GetEventTypeFor(typeof(TEvent)).Id], dimensions, _reopenedBy.Select(type => eventTypes.GetEventTypeFor(type).Id).ToArray(), _property);
    }

    ClosesStreamBuilder<TEvent> Include(ClosedStreamDimensions dimension)
    {
        _dimensions |= dimension;
        return this;
    }
}
