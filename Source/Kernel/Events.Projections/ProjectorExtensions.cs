// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Linq.Expressions;
using System.Reactive.Linq;

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Extension methods for building up a projection.
    /// </summary>
    public static class ProjectorExtensions
    {
        public static IObservable<EventContext> From(this IObservable<EventContext> observable, EventType eventType)
        {
            return observable.Where(_ => _.Event.Type == eventType);
        }

        public static IObservable<EventContext> RemovedWith(this IObservable<EventContext> observable, EventType eventType, KeyResolver keyResolver)
        {
            observable = observable.Where(_ => _.Event.Type == eventType);
            observable.Subscribe(_ =>
            {
                var key = keyResolver(_.Event);
                _.Changeset.ApplyRemove(key);
            });

            return observable;
        }

        public static IObservable<EventContext> Join(this IObservable<EventContext> observable, EventType eventType, PropertyAccessor propertyResolver, KeyResolver keyResolver)
        {
            observable = observable.Where(_ => _.Event.Type == eventType);
            return observable;
        }

        public static IObservable<EventContext> Children(this IProjector projection, Expression childrenPropertyAccessor)
        {
            // Create new projection for the child property... ??
            // Projection could take a source state / collection

            // Changes should be done through changesets (Add, Remove, Update)
            return projection.Event;
        }

        public static IObservable<EventContext> Project(this IObservable<EventContext> observable, string prefix, KeyResolver keyResolver, params Expression[] expressions)
        {
            observable.Subscribe(_ => Console.WriteLine($"{prefix} - Event : {_.Event.Type}"));
            return observable;
        }
    }
}
