// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Events.Projections.Changes;

namespace Cratis.Events.Projections
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

        public static IObservable<EventContext> Child(this IObservable<EventContext> observable, Property childrenProperty, Property identifiedByProperty, EventValueProvider keyResolver, IEnumerable<PropertyMapper> propertyMappers)
        {
            observable.Subscribe(_ =>
            {
                var items = _.Changeset.InitialState.EnsureCollection(childrenProperty);
                var key = keyResolver(_.Event);
                if (!items.Contains(identifiedByProperty, key))
                {
                    _.Changeset.ApplyAddChild(childrenProperty, identifiedByProperty, key, propertyMappers);
                }
            });
            return observable;
        }

        public static IObservable<EventContext> Project(this IObservable<EventContext> observable, Property childrenProperty, Property identifiedByProperty, InstanceAccessor instanceAccessor, EventValueProvider keyResolver, IEnumerable<PropertyMapper> propertyMappers)
        {
            if (childrenProperty.IsRoot)
            {
                observable.Subscribe(_ => _.Changeset.ApplyProperties(propertyMappers));
            }
            else
            {
                observable.Subscribe(_ =>
                {
                    var key = keyResolver(_.Event);
                    if (!_.Changeset.HasChildBeenAddedWithKey(childrenProperty, key))
                    {
                        _.Changeset.ApplyChildProperties(instanceAccessor, childrenProperty, identifiedByProperty, keyResolver, propertyMappers);
                    }
                });
            }
            return observable;
        }

        public static IObservable<EventContext> RemovedWith(this IObservable<EventContext> observable, EventType eventType)
        {
            observable.Where(_ => _.Event.Type == eventType).Subscribe(_ => _.Changeset.ApplyRemove());
            return observable;
        }

        public static IObservable<EventContext> Join(this IObservable<EventContext> observable, EventType eventType, PropertyAccessor propertyResolver)
        {
            return observable.Where(_ => _.Event.Type == eventType);
        }
    }
}
