// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reactive.Linq;
using Aksio.Cratis.Changes;
using Aksio.Cratis.Dynamic;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Events.Projections
{
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
        public static IObservable<EventContext> From(this IObservable<EventContext> observable, EventType eventType)
        {
            return observable.Where(_ => _.Event.Type == eventType);
        }

        /// <summary>
        /// Handle a child operation.
        /// </summary>
        /// <param name="observable"><see cref="IObservable{T}"/> to work with.</param>
        /// <param name="childrenProperty">The property in which children are stored on the object.</param>
        /// <param name="identifiedByProperty">The property that identifies a child.</param>
        /// <param name="keyResolver">The resolver for resolving the key from the event.</param>
        /// <param name="propertyMappers">PropertyMappers used to map from the event to the child object.</param>
        /// <returns>The observable for continuation.</returns>
        public static IObservable<EventContext> Child(
            this IObservable<EventContext> observable,
            PropertyPath childrenProperty,
            PropertyPath identifiedByProperty,
            ValueProvider<Event> keyResolver,
            IEnumerable<PropertyMapper<Event, ExpandoObject>> propertyMappers)
        {
            observable.Subscribe(_ =>
            {
                var items = _.Changeset.InitialState.EnsureCollection<ExpandoObject>(childrenProperty);
                var key = keyResolver(_.Event);
                if (!items.Contains(identifiedByProperty, key))
                {
                    _.Changeset.AddChild(childrenProperty, identifiedByProperty, key, propertyMappers);
                }
            });
            return observable;
        }

        /// <summary>
        /// Project properties from event onto model or child model.
        /// </summary>
        /// <param name="observable"><see cref="IObservable{T}"/> to work with.</param>
        /// <param name="childrenProperty">The property in which children are stored on the object.</param>
        /// <param name="identifiedByProperty">The property that identifies a child.</param>
        /// <param name="keyResolver">The resolver for resolving the key from the event.</param>
        /// <param name="propertyMappers">PropertyMappers used to map from the event to the child object.</param>
        /// <returns>The observable for continuation.</returns>
        public static IObservable<EventContext> Project(
            this IObservable<EventContext> observable,
            PropertyPath childrenProperty,
            PropertyPath identifiedByProperty,
            ValueProvider<Event> keyResolver,
            IEnumerable<PropertyMapper<Event, ExpandoObject>> propertyMappers)
        {
            if (childrenProperty.IsRoot)
            {
                observable.Subscribe(_ => _.Changeset.SetProperties(propertyMappers));
            }
            else
            {
                observable.Subscribe(_ =>
                {
                    var key = keyResolver(_.Event);
                    if (!_.Changeset.HasChildBeenAddedWithKey(childrenProperty, key))
                    {
                        var child = _.Changeset.GetChildByKey<ExpandoObject>(childrenProperty, identifiedByProperty, key);
                        _.Changeset.SetChildProperties(child, childrenProperty, identifiedByProperty, keyResolver, propertyMappers);
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
        public static IObservable<EventContext> RemovedWith(this IObservable<EventContext> observable, EventType eventType)
        {
            observable.Where(_ => _.Event.Type == eventType).Subscribe(_ => _.Changeset.Remove());
            return observable;
        }

        /// <summary>
        /// Join with a specific <see cref="EventType"/>.
        /// </summary>
        /// <param name="observable"><see cref="IObservable{T}"/> to work with.</param>
        /// <param name="eventType"><see cref="EventType"/> to join with.</param>
        /// <returns>The observable for continuation.</returns>
        public static IObservable<EventContext> Join(this IObservable<EventContext> observable, EventType eventType)
        {
            return observable.Where(_ => _.Event.Type == eventType);
        }
    }
}
