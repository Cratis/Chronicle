// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Reactive.Linq;

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Extension methods for building up a projection.
    /// </summary>
    public static class ProjectionExtensions
    {
        public static IObservable<EventContext> From(this IObservable<EventContext> projection, EventType eventType, Expression? keyStrategy = default)
        {
            var observable = projection.Where(_ => _.Event.EventType == eventType);
            return observable;
        }

        public static IObservable<EventContext> RemovedWith(this IObservable<EventContext> projection, EventType eventType, Expression? keyStrategy = default)
        {
            var observable = projection.Where(_ => _.Event.EventType == eventType);
            return observable;
        }

        public static IObservable<EventContext> Join(this IObservable<EventContext> projection, EventType eventType, Expression propertyAccessor, Expression? keyStrategy = default)
        {
            return projection;
        }

        public static IObservable<EventContext> Children(this IObservable<EventContext> projection, Expression childrenPropertyAccessor, Expression? keyStrategy = default)
        {
            // Create new projection for the child property... ??
            // Projection could take a source state / collection

            // Changes should be done through changesets (Add, Remove, Update)
            return projection;
        }

        public static IObservable<EventContext> Project(this IObservable<EventContext> observable, params Expression[] expressions)
        {
            observable.Subscribe(_ => { });
            return observable;
        }
    }
}
