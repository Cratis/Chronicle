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
using Cratis.Reflection;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Extension methods for building up a projection.
/// </summary>
public static class ProjectionExtensions
{
    /// <summary>
    /// Filter an observable for a specific <see cref="EventType"/>.
    /// </summary>
    /// <param name="observable"><see cref="IObservable{T}"/> to filter.</param>
    /// <param name="eventType"><see cref="EventType"/> to filter for.</param>
    /// <returns>Filtered <see cref="IObservable{T}"/>.</returns>
    public static IObservable<ProjectionEventContext> WhereEventTypeEquals(this IObservable<ProjectionEventContext> observable, EventType eventType)
    {
        return observable.Where(_ => _.Event.Metadata.Type.Id == eventType.Id);
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
    /// <returns>A new observable for the ResolveJoin operation.</returns>
    public static IObservable<ProjectionEventContext> ResolveJoin(
        this IObservable<ProjectionEventContext> observable,
        IEventSequenceStorage eventSequenceStorage,
        EventType joinEventType,
        PropertyPath onModelProperty)
    {
        var joinSubject = new Subject<ProjectionEventContext>();
        observable.Subscribe(_ =>
        {
            var onValue = onModelProperty.GetValue(_.Changeset.CurrentState, _.Key.ArrayIndexers);
            if (onValue is not null)
            {
                var hasInstance = eventSequenceStorage.HasInstanceFor(joinEventType.Id, onValue.ToString()!).GetAwaiter().GetResult();
                if (hasInstance)
                {
                    var lastEventInstance = eventSequenceStorage.GetLastInstanceFor(joinEventType.Id, onValue.ToString()!).GetAwaiter().GetResult();
                    var changeset = _.Changeset.ResolvedJoin(onModelProperty, _.Key.Value, lastEventInstance, _.Key.ArrayIndexers);
                    joinSubject.OnNext(_ with
                    {
                        Event = lastEventInstance,
                        Changeset = changeset
                    });
                }
            }
        });
        return joinSubject;
    }

    /// <summary>
    /// Optimize the changeset.
    /// </summary>
    /// <param name="observable"><see cref="IObservable{T}"/> to work with.</param>
    /// <returns>The observable for continuation.</returns>
    public static IObservable<ProjectionEventContext> Optimize(this IObservable<ProjectionEventContext> observable)
    {
        observable.Subscribe(_ => _.Changeset.Optimize());
        return observable;
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
            observable.Subscribe(_ => _.Changeset.SetProperties(propertyMappers, _.Key.ArrayIndexers));
        }
        else
        {
            observable.Subscribe(_ =>
            {
                if (_.Key.ArrayIndexers.HasFor(childrenProperty))
                {
                    var items = _.Changeset.InitialState.EnsureCollection<object>(childrenProperty, _.Key.ArrayIndexers);
                    var childrenPropertyIndexer = _.Key.ArrayIndexers.GetFor(childrenProperty);
                    if (!identifiedByProperty.IsSet ||
                        !items.Contains(identifiedByProperty, childrenPropertyIndexer.Identifier))
                    {
                        _.Changeset.AddChild<ExpandoObject>(childrenProperty, identifiedByProperty, childrenPropertyIndexer.Identifier, propertyMappers, _.Key.ArrayIndexers);
                        return;
                    }
                    _.Changeset.SetProperties(propertyMappers, _.Key.ArrayIndexers);
                }
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
    public static IObservable<ProjectionEventContext> RemoveChild(this IObservable<ProjectionEventContext> observable, PropertyPath childrenProperty, PropertyPath identifiedByProperty)
    {
        observable.Subscribe(_ =>
        {
            var items = _.Changeset.InitialState.EnsureCollection<object>(childrenProperty, _.Key.ArrayIndexers);
            var childrenPropertyIndexer = _.Key.ArrayIndexers.GetFor(childrenProperty);
            if (identifiedByProperty.IsSet &&
                items.Contains(identifiedByProperty, childrenPropertyIndexer.Identifier))
            {
                _.Changeset.RemoveChild(childrenProperty, identifiedByProperty, childrenPropertyIndexer.Identifier, _.Key.ArrayIndexers);
            }
        });
        return observable;
    }
}
