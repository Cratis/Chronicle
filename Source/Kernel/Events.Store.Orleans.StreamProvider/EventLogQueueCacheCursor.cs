// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Streams;

namespace Cratis.Events.Store.Orleans.StreamProvider
{
    public class EventLogQueueCacheCursor : IQueueCacheCursor
    {
        readonly IStreamIdentity _streamIdentity;
        readonly StreamSequenceToken _token;

        public EventLogQueueCacheCursor(IStreamIdentity streamIdentity, StreamSequenceToken token)
        {
            _streamIdentity = streamIdentity;
            _token = token;
        }

        /// <inheritdoc/>
        public IBatchContainer GetCurrent(out Exception exception)
        {
            exception = new NotImplementedException();
            return null!;
        }

        /// <inheritdoc/>
        public bool MoveNext()
        {
            return false;
        }

        /// <inheritdoc/>
        public void RecordDeliveryFailure()
        {
        }

        /// <inheritdoc/>
        public void Refresh(StreamSequenceToken token)
        {
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }
}
