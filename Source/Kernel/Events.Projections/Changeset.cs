// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Represents a changeset - the consequence of an individual handling of a <see cref="IProjection"/>.
    /// </summary>
    public class Changeset
    {
        readonly dynamic _initialState;
        readonly List<IChange> _changes = new();

        /// <summary>
        /// Initializes a new instance of <see cref="Changeset"/>.
        /// </summary>
        /// <param name="initialState">The initial state before any changes are applied.</param>
        public Changeset(dynamic initialState)
        {
            _initialState = initialState;
        }

        /// <summary>
        /// Gets all the changes for the changeset.
        /// </summary>
        public IEnumerable<IChange> Changes => _changes;

        /// <summary>
        /// Applies properties to the <see cref="Changeset"/>.
        /// </summary>
        /// <param name="expressions"><see cref="Expression">Expressions</see> representing properties being manipulated.</param>
        /// <remarks>
        /// This will run a diff against the initial state and only produce changes that are new.
        /// </remarks>
        public void ApplyProperties(IEnumerable<Expression> expressions)
        {
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
        }

        /// <summary>
        /// Apply a remove change to the <see cref="Changeset"/>.
        /// </summary>
        public void ApplyRemove()
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
