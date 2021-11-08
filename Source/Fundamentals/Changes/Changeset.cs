// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Changes
{
    /// <summary>
    /// Represents a changeset of changes that can occur to an object.
    /// </summary>
    /// <typeparam name="T">Type of object we're working on.</typeparam>
    public class Changeset<T>
    {
        readonly List<Change> _changes = new();

        /// <summary>
        /// Initializes a new instance of <see cref="Changeset{T}"/>.
        /// </summary>
        /// <param name="incoming"><see cref="Incoming"/> that the <see cref="Changeset{T}"/> is for.</param>
        /// <param name="initialState">The initial state before any changes are applied.</param>
        public Changeset(T incoming, ExpandoObject initialState)
        {
            Incoming = incoming;
            InitialState = initialState;
        }

        /// <summary>
        /// Gets the <see cref="Incoming"/> the <see cref="Changeset{T}"/> is for.
        /// </summary>
        public T Incoming { get; }

        /// <summary>
        /// Gets the initial state of before changes in changeset occurred.
        /// </summary>
        public ExpandoObject InitialState { get; }

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
