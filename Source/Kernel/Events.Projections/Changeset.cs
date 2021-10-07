// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Linq.Expressions;
using Cratis.Dynamic;

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Represents a changeset - the consequence of an individual handling of a <see cref="IProjector"/>.
    /// </summary>
    public class Changeset
    {
        readonly Event _event;
        readonly ExpandoObject _initialState;
        readonly List<Change> _changes = new();

        /// <summary>
        /// Initializes a new instance of <see cref="Changeset"/>.
        /// </summary>
        /// <param name="event"><see cref="Event"/> that the changeset is for.</param>
        /// <param name="initialState">The initial state before any changes are applied.</param>
        public Changeset(Event @event, ExpandoObject initialState)
        {
            _event = @event;
            _initialState = initialState;
        }

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
            var workingState = _initialState.Clone();
            foreach (var propertyMapper in propertyMappers)
            {
                propertyMapper(_event, workingState);
            }
        }

        /// <summary>
        /// Applies properties to the child in the model to the <see cref="Changeset"/>.
        /// </summary>
        /// <param name="childrenAccessor"><see cref="Expression"/> for accessing the children collection.</param>
        /// <param name="modelKey"><see cref="Expression"/> for accessing the model key.</param>
        /// <param name="key">The key value.</param>
        /// <param name="expressions"><see cref="Expression">Expressions</see> representing properties being manipulated.</param>
        public void ApplyChildProperties(Expression childrenAccessor, Expression modelKey, object key, IEnumerable<Expression> expressions)
        {
            var workingState = _initialState.Clone();
        }

        /// <summary>
        /// Apply a remove change to the <see cref="Changeset"/>.
        /// </summary>
        /// <param name="key">The key value.</param>
        public void ApplyRemove(object key)
        {
        }

        /// <summary>
        /// Apply a remove child change to the <see cref="Changeset"/>.
        /// </summary>
        /// <param name="childrenAccessor"><see cref="Expression"/> for accessing the children collection.</param>
        /// <param name="modelKey"><see cref="Expression"/> for accessing the model key.</param>
        /// <param name="key">The key value.</param>
        public void RemoveChild(Expression childrenAccessor, Expression modelKey, object key)
        {
        }
    }
}
