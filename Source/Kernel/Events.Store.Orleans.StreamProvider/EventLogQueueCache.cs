// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Streams;

namespace Events.Store.Orleans.Streams
{
    public class EventLogQueueCache : IQueueCache
    {
        /// <inheritdoc/>
        public void AddToCache(IList<IBatchContainer> messages)
        {
        }

        /// <inheritdoc/>
        public IQueueCacheCursor GetCacheCursor(IStreamIdentity streamIdentity, StreamSequenceToken token) => throw new NotImplementedException();

        /// <inheritdoc/>
        public int GetMaxAddCount() => 0;

        /// <inheritdoc/>
        public bool IsUnderPressure() => false;

        /// <inheritdoc/>
        public bool TryPurgeFromCache(out IList<IBatchContainer> purgedItems)
        {
            purgedItems = new List<IBatchContainer>();
            return true;
        }
    }
}
