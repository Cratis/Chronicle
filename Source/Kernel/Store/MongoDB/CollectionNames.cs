// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store.Observation;
using Orleans;

namespace Aksio.Cratis.Events.Store.MongoDB
{
    /// <summary>
    /// Internal class for holding all collection names.
    /// </summary>
    internal static class CollectionNames
    {
        /// <summary>
        /// The collection name that holds <see cref="FailedObserverState"/>.
        /// </summary>
        internal const string FailedObservers = "failed-observers";

        /// <summary>
        /// The collection that holds <see cref="ObserverState"/>.
        /// </summary>
        internal const string Observers = "observers";

        /// <summary>
        /// The collection that holds <see cref="ReminderEntry"/>.
        /// </summary>
        internal const string Reminders = "reminders";
    }
}
