// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Federation.for_FederationPullService.when_pulling;

public class and_the_limit_exceeds_the_maximum_batch_size : given.a_pull_service
{
    FederatedEventBatch _batch;

    void Establish()
    {
        Configure(new FederationLimits { MaxBatchSize = 2 });
        _grants.Add(GrantForCaller());
        RegisterEventType("order-placed", EventTypeVisibility.Public);
        for (ulong sequenceNumber = 0; sequenceNumber < 5; sequenceNumber++)
        {
            Append("order-placed", sequenceNumber);
        }
    }

    async Task Because() => _batch = await Pull(limit: 1000);

    [Fact] void should_deliver_no_more_than_the_maximum() => _batch.Events.Count().ShouldEqual(2);
    [Fact] void should_continue_after_the_last_delivered_event() => _batch.NextSequenceNumber.ShouldEqual(2UL);
}
