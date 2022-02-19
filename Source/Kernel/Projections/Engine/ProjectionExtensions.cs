// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reactive.Linq;
using Aksio.Cratis.Changes;
using Aksio.Cratis.Dynamic;
using Aksio.Cratis.Events.Store;
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
        public static IObservable<ProjectionEventContext> From(this IObservable<ProjectionEventContext> observable, EventType eventType)
        {
            return observable.Where(_ => _.Event.Metadata.Type == eventType);
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
                        var items = _.Changeset.InitialState.EnsureCollection<ExpandoObject>(childrenProperty, _.Key.ArrayIndexers);
                        var childrenPropertyIndexer = _.Key.ArrayIndexers.GetFor(childrenProperty);
                        if (!items.Contains(identifiedByProperty, childrenPropertyIndexer.Identifier))
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
            observable.Where(_ => _.Event.Metadata.Type == eventType).Subscribe(_ => _.Changeset.Remove());
            return observable;
        }

        /// <summary>
        /// Join with a specific <see cref="EventType"/>.
        /// </summary>
        /// <param name="observable"><see cref="IObservable{T}"/> to work with.</param>
        /// <param name="eventType"><see cref="EventType"/> to join with.</param>
        /// <returns>The observable for continuation.</returns>
        public static IObservable<ProjectionEventContext> Join(this IObservable<ProjectionEventContext> observable, EventType eventType)
        {
            return observable.Where(_ => _.Event.Metadata.Type == eventType);
        }
    }
}
