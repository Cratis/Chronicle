// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Aksio.Changes;
using Aksio.Dynamic;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Properties;

namespace Aksio.Cratis.Kernel.Engines.Projections;

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
    /// <returns>An observable for continuation.</returns>
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
    /// <param name="eventProvider"><see cref="IEventSequenceStorage"/> for getting the event in the past.</param>
    /// <param name="joinEventType">Type of event to be joined.</param>
    /// <param name="onModelProperty">The property on the model to join on.</param>
    /// <returns>The observable for continuation.</returns>
    public static IObservable<ProjectionEventContext> ResolveJoin(
        this IObservable<ProjectionEventContext> observable,
        IEventSequenceStorage eventProvider,
        EventType joinEventType,
        PropertyPath onModelProperty)
    {
        var joinSubject = new Subject<ProjectionEventContext>();
        observable.Subscribe(_ =>
        {
            var onValue = onModelProperty.GetValue(_.Changeset.CurrentState, ArrayIndexers.NoIndexers);
            if (onValue is not null)
            {
                var checkTask = eventProvider.HasInstanceFor(EventSequenceId.Log, joinEventType.Id, onValue.ToString()!);
                checkTask.Wait();
                if (checkTask.Result)
                {
                    var lastEventInstanceTask = eventProvider.GetLastInstanceFor(EventSequenceId.Log, joinEventType.Id, onValue.ToString()!);
                    lastEventInstanceTask.Wait();
                    var lastEventInstance = lastEventInstanceTask.Result;

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
    /// Remove item based on event.
    /// </summary>
    /// <param name="observable"><see cref="IObservable{T}"/> to work with.</param>
    /// <param name="eventType"><see cref="EventType"/> causing the remove.</param>
    /// <returns>The observable for continuation.</returns>
    public static IObservable<ProjectionEventContext> RemovedWith(this IObservable<ProjectionEventContext> observable, EventType eventType)
    {
        observable.Where(_ => _.Event.Metadata.Type.Id == eventType.Id).Subscribe(_ => _.Changeset.Remove());
        return observable;
    }
}
