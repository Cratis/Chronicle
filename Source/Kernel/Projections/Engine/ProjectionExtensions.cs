// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reactive.Linq;
using System.Text.Json;
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
        /// Handle a child operation.
        /// </summary>
        /// <param name="observable"><see cref="IObservable{T}"/> to work with.</param>
        /// <param name="childrenProperty">The property in which children are stored on the object.</param>
        /// <param name="parentIdentifiedByProperty">The property that identifies the child when the parent is also a child.</param>
        /// <param name="parentKeyResolver">The resolver for resolving the parent key from the event.</param>
        /// <param name="identifiedByProperty">The property that identifies a child.</param>
        /// <param name="keyResolver">The resolver for resolving the key from the event.</param>
        /// <param name="propertyMappers">PropertyMappers used to map from the event to the child object.</param>
        /// <returns>The observable for continuation.</returns>
        public static IObservable<ProjectionEventContext> Child(
            this IObservable<ProjectionEventContext> observable,
            PropertyPath childrenProperty,
            PropertyPath parentIdentifiedByProperty,
            ValueProvider<AppendedEvent> parentKeyResolver,
            PropertyPath identifiedByProperty,
            ValueProvider<AppendedEvent> keyResolver,
            IEnumerable<PropertyMapper<AppendedEvent, ExpandoObject>> propertyMappers)
        {
            observable.Subscribe(_ =>
            {
                var key = keyResolver(_.Event);
                var parentKey = parentIdentifiedByProperty.IsRoot ? default : parentKeyResolver(_.Event);
                var json = JsonSerializer.Serialize(_.Changeset.InitialState);

                var items = _.Changeset.InitialState.EnsureCollection<ExpandoObject>(childrenProperty);

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
        /// <param name="parentIdentifiedByProperty">The property that identifies the child when the parent is also a child.</param>
        /// <param name="identifiedByProperty">The property that identifies a child.</param>
        /// <param name="keyResolver">The resolver for resolving the key from the event.</param>
        /// <param name="propertyMappers">PropertyMappers used to map from the event to the child object.</param>
        /// <returns>The observable for continuation.</returns>
        public static IObservable<ProjectionEventContext> Project(
            this IObservable<ProjectionEventContext> observable,
            PropertyPath childrenProperty,
            PropertyPath parentIdentifiedByProperty,
            PropertyPath identifiedByProperty,
            ValueProvider<AppendedEvent> keyResolver,
            IEnumerable<PropertyMapper<AppendedEvent, ExpandoObject>> propertyMappers)
        {
            Console.WriteLine(parentIdentifiedByProperty);

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
                        var child = _.Changeset.GetChildByKey<ExpandoObject>(key);
                        if (child != default)
                        {
                            _.Changeset.SetChildProperties(child, childrenProperty, identifiedByProperty, keyResolver, propertyMappers);
                        }
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
