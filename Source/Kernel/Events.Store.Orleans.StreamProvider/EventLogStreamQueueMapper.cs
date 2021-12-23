// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Configuration;
using Orleans.Streams;

namespace Events.Store.Orleans.Streams
{
    public class EventLogStreamQueueMapper : HashRingBasedStreamQueueMapper
    {
        public EventLogStreamQueueMapper(HashRingStreamQueueMapperOptions options, string queueNamePrefix) : base(options, queueNamePrefix)
        {
        }
    }
}
