// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Changes
{
    /// <summary>
    /// Represents a changeset of changes that can occur to an object.
    /// </summary>
    /// <typeparam name="TSource">Type of the source object we are working from.</typeparam>
    /// <typeparam name="TTarget">Type of target object we are applying changes to.</typeparam>
    public class Changeset<TSource, TTarget>
    {
        readonly List<Change> _changes = new();

        /// <summary>
        /// Initializes a new instance of <see cref="Changeset{TSource, TTarget}"/>.
        /// </summary>
        /// <param name="incoming"><see cref="Incoming"/> that the <see cref="Changeset{TSource, TTarget}"/> is for.</param>
        /// <param name="initialState">The initial state before any changes are applied.</param>
        public Changeset(TSource incoming, TTarget initialState)
        {
            Incoming = incoming;
            InitialState = initialState;
        }

        /// <summary>
        /// Gets the <see cref="Incoming"/> the <see cref="Changeset{TSource, TTarget}"/> is for.
        /// </summary>
        public TSource Incoming { get; }

        /// <summary>
        /// Gets the initial state of before changes in changeset occurred.
        /// </summary>
        public TTarget InitialState { get; }

        /// <summary>
        /// Gets all the changes for the changeset.
        /// </summary>
        public IEnumerable<Change> Changes => _changes;

        /// <summary>
        /// Add a change to the changeset.
        /// </summary>
        /// <param name="change"><see cref="Change"/> to add.</param>
        public void Add(Change change)
        {
            _changes.Add(change);
        }
    }
}
