// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Extension methods for building up a projection.
    /// </summary>
    public static class ProjectionExtensions
    {
        public static IObservable<EventContext> From(this IObservable<EventContext> observable, EventType eventType)
        {
            return observable.Where(_ => _.Event.Type == eventType);
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

        public static IObservable<EventContext> Child(this IObservable<EventContext> observable, Property childrenProperty, Property identifiedByProperty, EventValueProvider keyResolver)
        {
            observable.Subscribe(_ => _.Changeset.ApplyChild(childrenProperty, identifiedByProperty, keyResolver(_.Event)));
            return observable;
        }

        public static IObservable<EventContext> Project(this IObservable<EventContext> observable, InstanceAccessor instanceAccessor, EventValueProvider keyResolver, IEnumerable<PropertyMapper> propertyMappers)
        {
            observable.Subscribe(_ => _.Changeset.ApplyProperties(instanceAccessor, keyResolver, propertyMappers));
            return observable;
        }
    }
}
