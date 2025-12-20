// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;
using Cratis.Reflection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Extension methods for building up a projection.
/// </summary>
public static class ProjectionEventContextExtensions
{
    /// <summary>
    /// Filter an observable for a specific <see cref="EventType"/>.
    /// </summary>
    /// <param name="observable"><see cref="IObservable{T}"/> to filter.</param>
    /// <param name="eventType"><see cref="EventType"/> to filter for.</param>
    /// <returns>Filtered <see cref="IObservable{T}"/>.</returns>
    public static IObservable<ProjectionEventContext> WhereEventTypeEquals(
        this IObservable<ProjectionEventContext> observable, EventType eventType)
    {
        return observable.Where(_ => _.Event.Context.EventType.Id == eventType.Id);
    }

    /// <summary>
    /// Join with an event.
    /// </summary>
    /// <param name="observable"><see cref="IObservable{T}"/> to work with.</param>
    /// <param name="onModelProperty">The property on the model to join on.</param>
    /// <returns>A new observable for the Join operation.</returns>
    public static IObservable<ProjectionEventContext> Join(
        this IObservable<ProjectionEventContext> observable,
        PropertyPath onModelProperty)
    {
        var joinSubject = new Subject<ProjectionEventContext>();
        observable.Subscribe(_ =>
        {
            var changeset = _.Changeset.Join(onModelProperty, _.Key.Value, _.Key.ArrayIndexers);
            joinSubject.OnNext(_ with
            {
                Changeset = changeset
            });
        });
        return joinSubject;
    }

    /// <summary>
    /// Resolve a join for events that has happened.
    /// </summary>
    /// <param name="observable"><see cref="IObservable{T}"/> to work with.</param>
    /// <param name="eventSequenceStorage"><see cref="IEventSequenceStorage"/> for getting the event in the past.</param>
    /// <param name="joinEventType">Type of event to be joined.</param>
    /// <param name="onModelProperty">The property on the model to join on.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>A new observable for the ResolveJoin operation.</returns>
    public static IObservable<ProjectionEventContext> ResolveJoin(
        this IObservable<ProjectionEventContext> observable,
        IEventSequenceStorage eventSequenceStorage,
        EventType joinEventType,
        PropertyPath onModelProperty,
        ILogger logger)
    {
        var joinSubject = new Subject<ProjectionEventContext>();
        observable.Subscribe(context =>
        {
            var onValue = onModelProperty.GetValue(context.Changeset.CurrentState, context.Key.ArrayIndexers);
            if (onValue is not null)
            {
                var tryGetLastEvent = eventSequenceStorage.TryGetLastEventBefore(
                    joinEventType.Id,
                    onValue.ToString()!,
                    context.EventSequenceNumber).GetAwaiter().GetResult();

                void HandleResolveJoin(Option<AppendedEvent> maybeLastEvent)
                {
                    if (!maybeLastEvent.HasValue) return;
                    var lastEvent = (AppendedEvent)maybeLastEvent;
                    var changeset = context.Changeset.ResolvedJoin(
                        onModelProperty,
                        context.Key.Value,
                        lastEvent,
                        context.Key.ArrayIndexers);
                    joinSubject.OnNext(context with
                    {
                        Event = lastEvent,
                        Changeset = changeset
                    });
                }

                // TODO: We need to have a fulfillment strategy here: https://github.com/Cratis/Chronicle/issues/50
                tryGetLastEvent.Switch(HandleResolveJoin, error =>
                {
#pragma warning disable CA1848
                    logger.LogError("Error when trying to resolve join: {Error}", error);
#pragma warning restore CA1848
                    throw error;
                });
            }
        });
        return joinSubject;
    }

    /// <summary>
    /// Project properties from event onto model or child model.
    /// </summary>
    /// <param name="observable"><see cref="IObservable{T}"/> to work with.</param>
    /// <param name="childrenProperty">The property in which children are stored on the object.</param>
    /// <param name="identifiedByProperty">The property that identifies a child.</param>
    /// <param name="propertyMappers">PropertyMappers used to map from the event to the child object.</param>
    /// <returns>The observable for continuation.</returns>
    public static IObservable<ProjectionEventContext> Project(
        this IObservable<ProjectionEventContext> observable,
        PropertyPath childrenProperty,
        PropertyPath identifiedByProperty,
        IEnumerable<PropertyMapper<AppendedEvent, ExpandoObject>> propertyMappers)
    {
        if (childrenProperty.IsRoot)
        {
            observable.Subscribe(context =>
                context.Changeset.SetProperties(propertyMappers, context.Key.ArrayIndexers));
        }
        else
        {
            observable.Subscribe(context =>
            {
                if (!context.Key.ArrayIndexers.HasFor(childrenProperty))
                {
                    return;
                }

                var items = context.Changeset.InitialState.EnsureCollection<object>(childrenProperty, context.Key.ArrayIndexers);
                var childrenPropertyIndexer = context.Key.ArrayIndexers.GetFor(childrenProperty);
                if (!context.IsJoin && (!identifiedByProperty.IsSet ||
                                        !items.Contains(identifiedByProperty, childrenPropertyIndexer.Identifier)))
                {
                    context.Changeset.AddChild<ExpandoObject>(
                        childrenProperty,
                        identifiedByProperty,
                        childrenPropertyIndexer.Identifier,
                        propertyMappers,
                        context.Key.ArrayIndexers);
                    return;
                }

                context.Changeset.SetProperties(propertyMappers, context.Key.ArrayIndexers);
            });
        }

        return observable;
    }

    /// <summary>
    /// Add a child from the value of an event property.
    /// </summary>
    /// <param name="observable"><see cref="IObservable{T}"/> to work with.</param>
    /// <param name="childrenProperty">The property in which children are stored on the object.</param>///
    /// <param name="valueProvider">The <see cref="ValueProvider{T}"/> for getting the value from the event.</param>
    /// <returns>The observable for continuation.</returns>
    public static IObservable<ProjectionEventContext> AddChildFromEventProperty(
        this IObservable<ProjectionEventContext> observable,
        PropertyPath childrenProperty,
        ValueProvider<AppendedEvent> valueProvider)
    {
        observable.Subscribe(_ =>
        {
            var value = valueProvider(_.Event);
            if (!value.GetType().IsAPrimitiveType())
            {
                value = value.AsExpandoObject();
            }

            _.Changeset.AddChild(childrenProperty, value);
        });

        return observable;
    }

    /// <summary>
    /// Remove item based on event.
    /// </summary>
    /// <param name="observable"><see cref="IObservable{T}"/> to work with.</param>
    /// <returns>The observable for continuation.</returns>
    public static IObservable<ProjectionEventContext> Remove(this IObservable<ProjectionEventContext> observable)
    {
        observable.Subscribe(_ => _.Changeset.Remove());
        return observable;
    }

    /// <summary>
    /// Remove child based on event.
    /// </summary>
    /// <param name="observable"><see cref="IObservable{T}"/> to work with.</param>
    /// <param name="childrenProperty">The property in which children are stored on the object.</param>
    /// <param name="identifiedByProperty">The property that identifies a child.</param>
    /// <returns>The observable for continuation.</returns>
    public static IObservable<ProjectionEventContext> RemoveChild(
        this IObservable<ProjectionEventContext> observable,
        PropertyPath childrenProperty,
        PropertyPath identifiedByProperty)
    {
        observable.Subscribe(_ =>
        {
            var items = _.Changeset.InitialState.EnsureCollection<object>(childrenProperty, _.Key.ArrayIndexers);
            var childrenPropertyIndexer = _.Key.ArrayIndexers.GetFor(childrenProperty);
            if (identifiedByProperty.IsSet &&
                items.Contains(identifiedByProperty, childrenPropertyIndexer.Identifier))
            {
                _.Changeset.RemoveChild(
                    childrenProperty,
                    identifiedByProperty,
                    childrenPropertyIndexer.Identifier,
                    _.Key.ArrayIndexers);
            }
        });
        return observable;
    }

    /// <summary>
    /// Remove children from all projections that has a child that is identified by the event.
    /// </summary>
    /// <param name="observable"><see cref="IObservable{T}"/> to work with.</param>
    /// <param name="childrenProperty">The property in which children are stored on the object.</param>
    /// <param name="identifiedByProperty">The property that identifies a child.</param>
    /// <returns>The observable for continuation.</returns>
    public static IObservable<ProjectionEventContext> RemoveChildFromAll(
        this IObservable<ProjectionEventContext> observable,
        PropertyPath childrenProperty,
        PropertyPath identifiedByProperty)
    {
        observable.Subscribe(_ => _.Changeset.RemoveChildFromAll(childrenProperty, identifiedByProperty, _.Key.Value!, _.Key.ArrayIndexers));
        return observable;
    }
}
