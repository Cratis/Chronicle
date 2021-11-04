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
        public ExpandoObject InitialState { get; }

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
        /// Apply a remove change to the <see cref="Changeset"/>.
        /// </summary>
        public void ApplyRemove()
        {
        }

        /// <summary>
        /// Applies properties to the child in the model to the <see cref="Changeset"/>.
        /// </summary>
        /// <param name="childrenAccessor"><see cref="PropertyAccessor"/> for accessing the children collection.</param>
        /// <param name="modelKey"><see cref="Expression"/> for accessing the model key.</param>
        /// <param name="key">The key value.</param>
        /// <param name="propertyMappers">Collection of <see cref="PropertyMapper">property mappers</see> that will manipulate properties on the target.</param>
        public void ApplyChildProperties(PropertyAccessor childrenAccessor, Expression modelKey, object key, IEnumerable<PropertyMapper> propertyMappers)
        {
            var workingState = InitialState.Clone();
        }

        /// <summary>
        /// Adds a child with values from.
        /// </summary>
        /// <param name="childrenAccessor"><see cref="PropertyAccessor"/> for accessing the children collection.</param>
        /// <param name="modelKey"><see cref="Expression"/> for accessing the model key.</param>
        /// <param name="key">The key value.</param>
        /// <param name="propertyMappers">Collection of <see cref="PropertyMapper">property mappers</see> that will manipulate properties on the target.</param>
        public void ApplyAddChild(Expression childrenAccessor, Expression modelKey, object key, IEnumerable<PropertyMapper> propertyMappers)
        {
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
