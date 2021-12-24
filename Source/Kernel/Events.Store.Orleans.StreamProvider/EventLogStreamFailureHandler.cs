// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Runtime;
using Orleans.Streams;

namespace Events.Store.Orleans.Streams
{
    public class EventLogStreamFailureHandler : IStreamFailureHandler
    {
        public EventLogStreamFailureHandler(QueueId queueId)
        {
        }

        public bool ShouldFaultSubsriptionOnError => true;

        public Task OnDeliveryFailure(GuidId subscriptionId, string streamProviderName, IStreamIdentity streamIdentity, StreamSequenceToken sequenceToken)
        {
            return Task.CompletedTask;
        }

        public Task OnSubscriptionFailure(GuidId subscriptionId, string streamProviderName, IStreamIdentity streamIdentity, StreamSequenceToken sequenceToken)
        {
            return Task.CompletedTask;
        }
    }
}
