// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Federation.for_FederationPullService.when_pulling;

public class and_one_link_is_throttled : given.a_pull_service
{
    FederatedEventBatch _throttledLink;
    FederatedEventBatch _otherLink;

    void Establish()
    {
        Configure(new FederationLimits { MaxEventsPerSecond = 2 });
        _grants.Add(GrantForCaller());
        _grants.Add(GrantForOtherPeer());
        RegisterEventType("order-placed", EventTypeVisibility.Public);
        for (ulong sequenceNumber = 0; sequenceNumber < 5; sequenceNumber++)
        {
            Append("order-placed", sequenceNumber);
        }
    }

    async Task Because()
    {
        await Pull();
        _throttledLink = await Pull(from: 5);

        _presented = _otherCertificate;
        _otherLink = await Pull();
    }

    [Fact] void should_throttle_the_link_that_used_up_its_budget() => _throttledLink.Status.ShouldEqual(FederatedEventBatchStatus.Throttled);
    [Fact] void should_not_affect_another_link() => _otherLink.Status.ShouldEqual(FederatedEventBatchStatus.Ok);
    [Fact] void should_serve_the_other_link_its_events() => _otherLink.Events.Count().ShouldEqual(5);
    [Fact] void should_not_record_throttling_for_the_other_link() => _status.DidNotReceive().RecordThrottled("link-b");
}
