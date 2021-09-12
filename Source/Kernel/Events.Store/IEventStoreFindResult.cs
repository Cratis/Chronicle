// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Cratis.Events.Store
{
    /// <summary>
    /// Defines the result of a find within the event store
    /// </summary>
    public interface IEventStoreFindResult
    {
        /// <summary>
        /// Gets an observable <see cref="Subject{T}"/> that will receive all the <see cref="CommittedEvent"/> from a find.
        /// </summary>
        Subject<CommittedEvent> Event { get; }

        /// <summary>
        /// Create an enumerable of all the <see cref="CommittedEvent"/> in the find result
        /// </summary>
        IEnumerable<CommittedEvent> AsEnumerable();
    }
}
