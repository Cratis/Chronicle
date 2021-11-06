// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;
using System.Linq.Expressions;
using Cratis.Dynamic;

namespace Cratis.Events.Projections.Changes
{
    /// <summary>
    /// Represents a changeset - the consequence of an individual handling of a <see cref="IProjection"/>.
    /// </summary>
    public class Changeset
    {
        readonly List<Change> _changes = new();

        /// <summary>
        /// Initializes a new instance of <see cref="Changeset"/>.
        /// </summary>
        /// <param name="event"><see cref="Event"/> that the <see cref="Changeset"/> is for.</param>
        /// <param name="initialState">The initial state before any changes are applied.</param>
        public Changeset(Event @event, ExpandoObject initialState)
        {
            Event = @event;
            InitialState = initialState;
        }

        /// <summary>
        /// Gets the <see cref="Event"/> the <see cref="Changeset"/> is for.
        /// </summary>
        public Event Event { get; }

        /// <summary>
        /// Gets the initial state of before changes in changeset occurred.
        /// </summary>
        public ExpandoObject InitialState { get; private set; }

        /// <summary>
        /// Gets all the changes for the changeset.
        /// </summary>
        public IEnumerable<Change> Changes => _changes;

        /// <summary>
        /// Applies properties to the <see cref="Changeset"/>.
        /// </summary>
        /// <param name="propertyMappers">Collection of <see cref="PropertyMapper">property mappers</see> that will manipulate properties on the target.</param>
        /// <remarks>
        /// This will run a diff against the initial state and only produce changes that are new.
        /// </remarks>
        public void ApplyProperties(IEnumerable<PropertyMapper> propertyMappers)
        {
            var workingState = InitialState.Clone();
            foreach (var propertyMapper in propertyMappers)
            {
                propertyMapper(Event, workingState);
            }

            var comparer = new ObjectsComparer.Comparer<ExpandoObject>();
            if (!comparer.Compare(InitialState, workingState, out var differences))
            {
                _changes.Add(new PropertiesChanged(workingState, differences.Select(_ => new PropertyDifference(InitialState, workingState, _))));
            }
        }

        /// <summary>
        /// Applies properties for a child to the <see cref="Changeset"/>.
        /// </summary>
        /// <param name="instanceAccessor"><see cref="InstanceAccessor"/> for accessing the actual instance to apply for.</param>
        /// <param name="childrenProperty">The <see cref="Property"/> on the parent that holds the children.</param>
        /// <param name="identifiedByProperty">The <see cref="Property"/> on the instance that identifies the child.</param>
        /// <param name="keyResolver">The <see cref="EventValueProvider"/> for resolving the key on the event.</param>
        /// <param name="propertyMappers">Collection of <see cref="PropertyMapper">property mappers</see> that will manipulate properties on the target.</param>
        /// <remarks>
        /// This will run a diff against the initial state and only produce changes that are new.
        /// </remarks>
        public void ApplyChildProperties(
            InstanceAccessor instanceAccessor,
            Property childrenProperty,
            Property identifiedByProperty,
            EventValueProvider keyResolver,
            IEnumerable<PropertyMapper> propertyMappers)
        {
            var workingState = InitialState.Clone();
            var workingInstance = instanceAccessor(workingState, Event, keyResolver);
            var initialInstance = workingInstance.Clone();
            foreach (var propertyMapper in propertyMappers)
            {
                propertyMapper(Event, workingInstance);
            }

            var comparer = new ObjectsComparer.Comparer<ExpandoObject>();
            if (!comparer.Compare(initialInstance, workingInstance, out var differences))
            {
                _changes.Add(new ChildPropertiesChanged(
                    workingInstance,
                    childrenProperty,
                    identifiedByProperty,
                    keyResolver(Event),
                    differences.Select(_ => new PropertyDifference(initialInstance, workingInstance, _))));
            }
        }

        /// <summary>
        /// Apply a remove change to the <see cref="Changeset"/>.
        /// </summary>
        public void ApplyRemove()
        {
        }

        /// <summary>
        /// Applies properties to the child in the model to the <see cref="Changeset"/>.
        /// </summary>
        /// <param name="childrenProperty"><see cref="Property"/> for accessing the children collection.</param>
        /// <param name="identifiedByProperty"><see cref="Property"/> that identifies the child.</param>
        /// <param name="key">Key value.</param>
        /// <exception cref="ChildrenPropertyIsNotEnumerable">Thrown when children property is not enumerable.</exception>
        public void ApplyAddChild(Property childrenProperty, Property identifiedByProperty, object key)
        {
            var workingState = InitialState.Clone();
            var inner = workingState.EnsurePath(childrenProperty) as IDictionary<string, object>;
            if (!inner.ContainsKey(childrenProperty.LastSegment))
            {
                inner[childrenProperty.LastSegment] = new List<ExpandoObject>();
            }

            if (!(inner[childrenProperty.LastSegment] is IEnumerable))
            {
                throw new ChildrenPropertyIsNotEnumerable(childrenProperty);
            }

            var items = (inner[childrenProperty.LastSegment] as IEnumerable)!.Cast<ExpandoObject>();
            if (items is not IList<ExpandoObject>)
            {
                items = new List<ExpandoObject>(items!);
            }
            inner[childrenProperty.LastSegment] = items;

            if (!items!.Any((IDictionary<string, object> _) => _.ContainsKey(identifiedByProperty.Path) && _[identifiedByProperty.Path].Equals(key)))
            {
                var item = new ExpandoObject();
                identifiedByProperty.SetValue(item, key);
                ((IList<ExpandoObject>)items).Add(item);

                _changes.Add(new ChildAdded(item, childrenProperty, identifiedByProperty, key!));
            }

            InitialState = workingState;
        }

        /// <summary>
        /// Apply a remove child change to the <see cref="Changeset"/>.
        /// </summary>
        /// <param name="childrenAccessor"><see cref="Expression"/> for accessing the children collection.</param>
        /// <param name="modelKey"><see cref="Expression"/> for accessing the model key.</param>
        /// <param name="key">The key value.</param>
        public void ApplyRemoveChild(Expression childrenAccessor, Expression modelKey, object key)
        {
        }
    }
}
