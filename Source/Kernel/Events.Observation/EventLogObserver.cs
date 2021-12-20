// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events.Store;

namespace Cratis.Events.Observation
{
    public class EventLogObserver : IEventLogObserver
    {
        public void Next(AppendedEvent @event)
        {
            // Deal with the actual client call
            // gRPC bi-directional call
            // Server sends on stream to client
            // Waits for an acknowledge of clients completion
            // If timeout - quarantine observer for its partition
            // If failure - quarantine observer for its partition
        }
    }
}
