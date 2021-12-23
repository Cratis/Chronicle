// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Runtime;
using Orleans.Streams;

namespace Events.Store.Orleans.Streams
{
    public class EventLogStreamFailureHandler : IStreamFailureHandler
    {
        public bool ShouldFaultSubsriptionOnError => throw new NotImplementedException();

        public Task OnDeliveryFailure(GuidId subscriptionId, string streamProviderName, IStreamIdentity streamIdentity, StreamSequenceToken sequenceToken) => throw new NotImplementedException();
        public Task OnSubscriptionFailure(GuidId subscriptionId, string streamProviderName, IStreamIdentity streamIdentity, StreamSequenceToken sequenceToken) => throw new NotImplementedException();
    }
}
