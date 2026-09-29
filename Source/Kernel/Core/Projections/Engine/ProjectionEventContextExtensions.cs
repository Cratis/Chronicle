// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Dynamic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Reflection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine;

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
        PropertyPath onModelProperty) => Join(observable, onModelProperty, false);

    /// <summary>
    /// Join with an event, preserving whether a keyed From at this level also consumes it.
    /// </summary>
    /// <param name="observable"><see cref="IObservable{T}"/> to work with.</param>
    /// <param name="onModelProperty">The property on the model to join on.</param>
    /// <param name="hasKeyedFrom">Whether the same event is also projected through a keyed From at this level.</param>
    /// <returns>A new observable for the Join operation.</returns>
    public static IObservable<ProjectionEventContext> Join(
        this IObservable<ProjectionEventContext> observable,
        PropertyPath onModelProperty,
        bool hasKeyedFrom)
    {
        return Observable.Create<ProjectionEventContext>(observer =>
            observable.Subscribe(
                _ =>
                {
                    var changeset = _.Changeset.Join(onModelProperty, _.JoinKey ?? _.Key.Value, _.Key.ArrayIndexers);
                    if (hasKeyedFrom)
                    {
                        // Join creates the outer change before the child changeset receives its mappings.
                        // Preserve the From origin so sinks can distinguish a keyed write from a join-only
                        // event whose direct all-event changes must not upsert a phantom root.
                        _.Changeset.Changes.OfType<Joined>().Last().HasKeyedFrom = true;
                    }

                    observer.OnNext(_ with { Changeset = changeset });
                },
                observer.OnError,
                observer.OnCompleted));
    }

    /// <summary>
    /// Resolve a join for events that has happened.
    /// </summary>
    /// <param name="observable"><see cref="IObservable{T}"/> to work with.</param>
    /// <param name="eventSequenceStorage"><see cref="IEventSequenceStorage"/> for getting the event in the past.</param>
    /// <param name="joinEventType">Type of event to be joined.</param>
    /// <param name="onModelProperty">The property on the model to join on.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="eventCompliance">Optional handler for releasing a stored join event's compliance and security fields before projecting it.</param>
    /// <param name="joinEventSchema">Optional schema for the stored join event.</param>
    /// <param name="eventTypesStorage">Optional <see cref="IEventTypesStorage"/> to resolve the schema of the generation a stored join event was stored at, when it differs from <paramref name="joinEventSchema"/>.</param>
    /// <returns>A new observable for the ResolveJoin operation.</returns>
    public static IObservable<ProjectionEventContext> ResolveJoin(
        this IObservable<ProjectionEventContext> observable,
        IEventSequenceStorage eventSequenceStorage,
        EventType joinEventType,
        PropertyPath onModelProperty,
        ILogger logger,
        IEventCompliance? eventCompliance = null,
        JsonSchema? joinEventSchema = null,
        IEventTypesStorage? eventTypesStorage = null)
    {
        var schemasByStoredEventType = new ConcurrentDictionary<EventType, JsonSchema?>();

        // Note: TryGetLastEventBefore is awaited synchronously here because this runs inside the
        // synchronous Rx Subject pipeline. HandleEvent.Perform commits the changeset immediately
        // after projection.OnNext() returns, so the join resolution must complete synchronously.
        // A proper async refactor would require restructuring the entire projection pipeline.
        // See: https://github.com/Cratis/Chronicle/issues/50
        //
        // The Task.Run wrapper hops to a thread-pool thread before blocking on GetResult().
        // Without it, EF Core continuations targeting a captured SynchronizationContext (which on
        // Orleans is the grain's task scheduler the current thread is already blocked on) would
        // deadlock — SQLite's in-process I/O completes synchronously enough to hide this, but
        // PostgreSQL / SQL Server reliably hang every join-event subscriber the first time the
        // continuation tries to schedule itself back onto the blocked scheduler.
        return Observable.Create<ProjectionEventContext>(observer =>
            observable.Subscribe(
                context =>
                {
                    var onValue = onModelProperty.GetValue(context.Changeset.CurrentState, context.Key.ArrayIndexers);
                    if (onValue is null) return;

#pragma warning disable CA2007
                    var tryGetLastEvent = Task.Run(() => eventSequenceStorage.TryGetLastEventBefore(
                        joinEventType.Id,
                        onValue.ToString()!,
                        context.EventSequenceNumber)).GetAwaiter().GetResult();
#pragma warning restore CA2007

                    tryGetLastEvent.Switch(
                        maybeLastEvent =>
                        {
                            if (!maybeLastEvent.HasValue) return;
                            var lastEvent = (AppendedEvent)maybeLastEvent;

                            // The stored join event is released with the schema of the generation it was stored at, which
                            // is not necessarily the one the projection was built with. Resolved once per generation.
                            var storedEventSchema = eventTypesStorage is null
                                ? joinEventSchema
                                : schemasByStoredEventType.GetOrAdd(
                                    lastEvent.Context.EventType,
                                    static (storedEventType, resolution) => Task.Run(() => resolution.EventTypes.GetStoredSchemaFor(storedEventType)).GetAwaiter().GetResult() ?? resolution.Fallback,
                                    (EventTypes: eventTypesStorage, Fallback: joinEventSchema));

                            if (eventCompliance is not null &&
                                storedEventSchema?.HasSchemaMetadata() == true &&
                                lastEvent.Context.Subject?.IsSet == true)
                            {
#pragma warning disable CA2007
                                lastEvent = Task.Run(() => eventCompliance.Release(lastEvent, storedEventSchema)).GetAwaiter().GetResult();
#pragma warning restore CA2007
                            }

                            var changeset = context.Changeset.ResolvedJoin(
                                onModelProperty,
                                context.Key.Value,
                                lastEvent,
                                context.Key.ArrayIndexers);
                            observer.OnNext(context with
                            {
                                Event = lastEvent,
                                Changeset = changeset
                            });
                        },
                        error =>
                        {
#pragma warning disable CA1848
                            logger.LogError("Error when trying to resolve join: {Error}", error);
#pragma warning restore CA1848
                            observer.OnError(error);
                        });
                },
                observer.OnError,
                observer.OnCompleted));
    }

    /// <summary>
    /// Project properties from event onto model or child model.
    /// </summary>
    /// <param name="observable"><see cref="IObservable{T}"/> to work with.</param>
    /// <param name="childrenProperty">The property in which children are stored on the object.</param>
    /// <param name="identifiedByProperty">The property that identifies a child.</param>
    /// <param name="propertyMappers">PropertyMappers used to map from the event to the child object.</param>
    /// <param name="childInitialState">Optional initial state for new child items. Used to pre-initialize nested collections.</param>
    /// <param name="subscriptions">Optional <see cref="CompositeDisposable"/> that receives ownership of the subscription, enabling explicit disposal.</param>
    /// <returns>The observable for continuation.</returns>
    public static IObservable<ProjectionEventContext> Project(
        this IObservable<ProjectionEventContext> observable,
        PropertyPath childrenProperty,
        PropertyPath identifiedByProperty,
        IEnumerable<PropertyMapper<AppendedEvent, ExpandoObject>> propertyMappers,
        ExpandoObject? childInitialState = null,
        CompositeDisposable? subscriptions = null)
    {
        IDisposable subscription;
        if (childrenProperty.IsRoot)
        {
            subscription = observable.Subscribe(context =>
                context.Changeset.SetProperties(propertyMappers, context.Key.ArrayIndexers));
        }
        else
        {
            subscription = observable.Subscribe(context =>
            {
                if (!context.Key.ArrayIndexers.HasFor(childrenProperty))
                {
                    return;
                }

                // The resolved key may target a collection deeper than this one (e.g. an event referenced at
                // multiple levels of a self-referential model that resolved to a nested node). In that case the
                // deeper-level projection applies it; applying here would wrongly mutate an ancestor node.
                if (KeyTargetsDeeperCollection(context.Key.ArrayIndexers, childrenProperty))
                {
                    return;
                }

                var items = context.Changeset.InitialState.EnsureCollection<object>(childrenProperty, context.Key.ArrayIndexers);
                var childrenPropertyIndexer = context.Key.ArrayIndexers.GetFor(childrenProperty);
                if (!context.IsJoin && (!identifiedByProperty.IsSet ||
                                        !items.Contains(identifiedByProperty, childrenPropertyIndexer.Identifier)))
                {
                    // AddChild applies the property mappers itself - it has to, because the ChildAdded change it
                    // records is what carries the new child's values all the way to the sink. Mapping again here
                    // is invisible for a plain set (it writes the same value twice) but runs every accumulating
                    // mapper a second time, so [AddFrom]/[SubtractFrom] on a child doubled on the very event that
                    // created it (#3940).
                    context.Changeset.AddChild<ExpandoObject>(
                        childrenProperty,
                        identifiedByProperty,
                        childrenPropertyIndexer.Identifier,
                        propertyMappers,
                        context.Key.ArrayIndexers,
                        childInitialState);
                    return;
                }

                context.Changeset.SetProperties(propertyMappers, context.Key.ArrayIndexers);
            });
        }

        subscriptions?.Add(subscription);
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
        return observable.Do(_ =>
        {
            var value = valueProvider(_.Event);
            if (!value.GetType().IsAPrimitiveType())
            {
                value = value.AsExpandoObject();
            }

            _.Changeset.AddChild(childrenProperty, value);
        });
    }

    /// <summary>
    /// Remove item based on event.
    /// </summary>
    /// <param name="observable"><see cref="IObservable{T}"/> to work with.</param>
    /// <returns>The observable for continuation.</returns>
    public static IObservable<ProjectionEventContext> Remove(this IObservable<ProjectionEventContext> observable) =>
        observable.Do(_ => _.Changeset.Remove());

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
        return observable.Do(_ =>
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
        PropertyPath identifiedByProperty) =>
        observable.Do(_ => _.Changeset.RemoveChildFromAll(childrenProperty, identifiedByProperty, _.Key.Value, _.Key.ArrayIndexers));

    /// <summary>
    /// Project properties from event onto a nested single-object model.
    /// The property mappers must already include the full path prefix to the nested object.
    /// </summary>
    /// <param name="observable"><see cref="IObservable{T}"/> to work with.</param>
    /// <param name="propertyMappers">PropertyMappers used to map from the event to the nested object (paths include nested prefix).</param>
    /// <returns>The observable for continuation.</returns>
    public static IObservable<ProjectionEventContext> ProjectNested(
        this IObservable<ProjectionEventContext> observable,
        IEnumerable<PropertyMapper<AppendedEvent, ExpandoObject>> propertyMappers) =>
        observable.Do(context => context.Changeset.SetProperties(propertyMappers, context.Key.ArrayIndexers));

    /// <summary>
    /// Clear (set to null) a nested single-object property based on event.
    /// </summary>
    /// <param name="observable"><see cref="IObservable{T}"/> to work with.</param>
    /// <param name="nestedProperty">The property path to the nested object on the parent.</param>
    /// <returns>The observable for continuation.</returns>
    public static IObservable<ProjectionEventContext> ClearNested(
        this IObservable<ProjectionEventContext> observable,
        PropertyPath nestedProperty) =>
        observable.Do(_ => _.Changeset.ClearNested(nestedProperty, _.Key.ArrayIndexers));

    /// <summary>
    /// Determines whether the resolved key targets a collection strictly deeper than <paramref name="childrenProperty"/>,
    /// meaning a deeper-level projection is responsible for applying the change.
    /// </summary>
    /// <param name="arrayIndexers">The <see cref="ArrayIndexers"/> from the resolved key.</param>
    /// <param name="childrenProperty">The children property of the projection level being considered.</param>
    /// <returns>True if a deeper collection is targeted; otherwise false.</returns>
    static bool KeyTargetsDeeperCollection(ArrayIndexers arrayIndexers, PropertyPath childrenProperty)
    {
        var childSegments = childrenProperty.Segments.ToArray();
        foreach (var indexerSegments in arrayIndexers.All.Select(_ => _.ArrayProperty.Segments.ToArray()))
        {
            if (indexerSegments.Length <= childSegments.Length)
            {
                continue;
            }

            var isDescendant = true;
            for (var index = 0; index < childSegments.Length; index++)
            {
                if (!Equals(indexerSegments[index].Value, childSegments[index].Value))
                {
                    isDescendant = false;
                    break;
                }
            }

            if (isDescendant)
            {
                return true;
            }
        }

        return false;
    }
}
