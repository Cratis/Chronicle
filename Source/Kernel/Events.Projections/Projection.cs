// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Cratis.Events.Projections
{
    public interface IProjectionCollection
    {
    }

    public static class ProjectionExtensions
    {
        public static IObservable<Event> From(this IProjection projection, string eventType, IKeyStrategy? keyStrategy = default)
        {
            var observable = projection.Subject.Where(_ => _.EventType == eventType);
            return observable;
        }

        public static IObservable<Event> RemovedWith(this IProjection projection, string eventType, IKeyStrategy? keyStrategy = default)
        {
            var observable = projection.Subject.Where(_ => _.EventType == eventType);
            return observable;
        }

        public static IObservable<Event> Join(this IProjection projection, string eventType, Expression propertyAccessor, Expression? keyStrategy = default)
        {
            return projection.Subject;
        }

        public static IObservable<Event> Children(this IProjection projection, Expression childrenPropertyAccessor, Expression? keyStrategy = default)
        {
            // Create new projection for the child property... ??
            // Projection could take a source state / collection

            // Changes should be done through changesets (Add, Remove, Update)
            return projection.Subject;
        }

        public static IObservable<Event> ProjectTo(this IObservable<Event> observable)
        {
            observable.Subscribe(_ => { });
            return observable;
        }
    }

    public record Event(uint SequenceNumber, string EventType, DateTimeOffset Occurred, string EventSourceId, object Content);

    public interface IProjection
    {
        ISubject<Event> Subject { get; }
    }

    public class Projection
    {
        public ISubject<Event>? Subject { get; }

        /*
        Thoughts:

        - Observable
            .

        */
    }
}
