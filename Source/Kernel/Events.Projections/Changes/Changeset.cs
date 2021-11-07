// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        /// <param name="item">THe item to add from.</param>
        /// <param name="childrenProperty">The <see cref="Property"/> on the parent that holds the children.</param>
        /// <param name="identifiedByProperty">The <see cref="Property"/> on the instance that identifies the child.</param>
        /// <param name="keyResolver">The <see cref="EventValueProvider"/> for resolving the key on the event.</param>
        /// <param name="propertyMappers">Collection of <see cref="PropertyMapper">property mappers</see> that will manipulate properties on the target.</param>
        /// <remarks>
        /// This will run a diff against the initial state and only produce changes that are new.
        /// </remarks>
        public void ApplyChildProperties(
            ExpandoObject item,
            Property childrenProperty,
            Property identifiedByProperty,
            EventValueProvider keyResolver,
            IEnumerable<PropertyMapper> propertyMappers)
        {
            var workingItem = item.Clone();
            foreach (var propertyMapper in propertyMappers)
            {
                propertyMapper(Event, workingItem);
            }

            var comparer = new ObjectsComparer.Comparer<ExpandoObject>();
            if (!comparer.Compare(item, workingItem, out var differences))
            {
                _changes.Add(new ChildPropertiesChanged(
                    workingItem,
                    childrenProperty,
                    identifiedByProperty,
                    keyResolver(Event),
                    differences.Select(_ => new PropertyDifference(item, workingItem, _))));
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
        /// <param name="propertyMappers">Collection of <see cref="PropertyMapper">property mappers</see> that will manipulate properties on the target.</param>
        /// <exception cref="ChildrenPropertyIsNotEnumerable">Thrown when children property is not enumerable.</exception>
        public void ApplyAddChild(Property childrenProperty, Property identifiedByProperty, object key, IEnumerable<PropertyMapper> propertyMappers)
        {
            var workingState = InitialState.Clone();
            var items = workingState.EnsureCollection(childrenProperty);

            if (!items.Contains(identifiedByProperty, key))
            {
                var item = new ExpandoObject();

                foreach (var propertyMapper in propertyMappers)
                {
                    propertyMapper(Event, item);
                }

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
