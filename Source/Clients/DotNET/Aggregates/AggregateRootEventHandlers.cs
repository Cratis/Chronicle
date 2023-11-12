// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using Aksio.Cratis.Conventions;
using Aksio.Cratis.Events;

namespace Aksio.Cratis.Aggregates;

#pragma warning disable MA0048 // File name must match type name

/// <summary>
/// Represents a method signature that handles an event asynchronous for an <see cref="AggregateRoot"/>.
/// </summary>
/// <param name="event">Event to handle.</param>
/// <typeparam name="T">Type of event.</typeparam>
/// <returns>Awaitable task.</returns>
public delegate Task AsyncAggregateRootEventHandler<T>(T @event);

/// <summary>
/// Represents a method signature that handles an event asynchronous for an <see cref="AggregateRoot"/>.
/// </summary>
/// <param name="event">Event to handle.</param>
/// <param name="context"><see cref="EventContext"/>.</param>
/// <typeparam name="T">Type of event.</typeparam>
/// <returns>Awaitable task.</returns>
public delegate Task AsyncAggregateRootEventHandlerWithContext<T>(T @event, EventContext context);

/// <summary>
/// Represents a method signature that handles an event synchronous for an <see cref="AggregateRoot"/>.
/// </summary>
/// <param name="event">Event to handle.</param>
/// <typeparam name="T">Type of event.</typeparam>
public delegate void SyncAggregateRootEventHandler<T>(T @event);

/// <summary>
/// Represents a method signature that handles an event synchronous for an <see cref="AggregateRoot"/>.
/// </summary>
/// <param name="event">Event to handle.</param>
/// <param name="context"><see cref="EventContext"/>.</param>
/// <typeparam name="T">Type of event.</typeparam>
public delegate void SyncAggregateRootEventHandlerWithContext<T>(T @event, EventContext context);
#pragma warning restore MA0048 // File name must match type name

/// <summary>
/// Represents the event handlers for an <see cref="AggregateRoot"/>.
/// </summary>
public class AggregateRootEventHandlers
{
    static readonly IEnumerable<ConventionSignature> _conventionMethods = new[]
    {
        new ConventionSignature(typeof(AsyncAggregateRootEventHandler<>)),
        new ConventionSignature(typeof(AsyncAggregateRootEventHandlerWithContext<>)),
        new ConventionSignature(typeof(SyncAggregateRootEventHandler<>)),
        new ConventionSignature(typeof(SyncAggregateRootEventHandlerWithContext<>))
    };

    readonly TypeWithConventionSignatures _typeWithConventionSignatures;

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateRootEventHandlers"/> class.
    /// </summary>
    /// <param name="aggregateRootType">Type of <see cref="IAggregateRoot"/>.</param>
    public AggregateRootEventHandlers(Type aggregateRootType)
    {
        _typeWithConventionSignatures = new TypeWithConventionSignatures(
            aggregateRootType,
            _conventionMethods,
            HasEventAsFirstParameter);

        EventTypes = _typeWithConventionSignatures.Methods.Select(_ => _.Method.GetParameters()[0].ParameterType).Select(_ => _.GetEventType()).ToImmutableList();
    }

    /// <summary>
    /// Gets whether or not it has any handle methods.
    /// </summary>
    public bool HasHandleMethods => _typeWithConventionSignatures.Methods.Count > 0;

    /// <summary>
    /// Gets a collection of <see cref="EventType">event types</see> that can be handled.
    /// </summary>
    public IImmutableList<EventType> EventTypes { get; }

    /// <summary>
    /// Handle a collection of events.
    /// </summary>
    /// <param name="target">The target <see cref="IAggregateRoot"/> to handle for.</param>
    /// <param name="events">Collection of <see cref="AppendedEvent"/> to handle.</param>
    /// <returns>Awaitable task.</returns>
    public async Task Handle(IAggregateRoot target, IEnumerable<EventAndContext> events)
    {
        foreach (var eventAndContext in events)
        {
            if (_typeWithConventionSignatures.CanInvoke(eventAndContext.Event, eventAndContext.Context))
            {
                await _typeWithConventionSignatures.Invoke(target, eventAndContext.Event, eventAndContext.Context);
            }
        }
    }

    bool HasEventAsFirstParameter(MethodInfo methodInfo) =>
        methodInfo.GetParameters().FirstOrDefault()?.ParameterType.IsEventType() ?? false;
}
