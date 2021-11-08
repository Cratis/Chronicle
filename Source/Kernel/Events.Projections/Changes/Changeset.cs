// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

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
