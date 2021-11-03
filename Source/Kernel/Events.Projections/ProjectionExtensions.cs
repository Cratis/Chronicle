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
            observable = observable.Where(_ => _.Event.Type == eventType);
            observable.Subscribe(_ => _.Changeset.ApplyRemove());

            return observable;
        }

        public static IObservable<EventContext> Join(this IObservable<EventContext> observable, EventType eventType, PropertyAccessor propertyResolver)
        {
            return observable.Where(_ => _.Event.Type == eventType);
        }

        public static IObservable<EventContext> Children(this IObservable<EventContext> observable, PropertyAccessor childrenPropertyAccessor)
        {
            // Create new projection for the child property... ??
            // Projection could take a source state / collection

            // Changes should be done through changesets (Add, Remove, Update)
            return observable;
        }

        public static IObservable<EventContext> Project(this IObservable<EventContext> observable, IEnumerable<PropertyMapper> propertyMappers)
        {
            observable.Subscribe(_ => _.Changeset.ApplyProperties(propertyMappers));
            return observable;
        }
    }
}
