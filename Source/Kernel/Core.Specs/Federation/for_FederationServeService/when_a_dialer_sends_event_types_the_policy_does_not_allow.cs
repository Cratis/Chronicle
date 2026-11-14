// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Federation;

namespace Cratis.Chronicle.Federation.for_FederationServeService;

public class when_a_dialer_sends_event_types_the_policy_does_not_allow : given.a_serve_service
{
    async Task Establish()
    {
        await SetPolicy("acme", FederationDirection.AcceptorInbound, false, "type-a");
        _presented = await Issued("acme");
    }

    async Task Because() => await Dial(
        request => new ServeBatch
        {
            RequestId = request.RequestId,
            Batch = new()
            {
                Status = FederatedEventBatchStatus.Ok,
                OriginEventStore = "customer-store",
                Events = [Event(0UL, "type-a"), Event(1UL, "type-b"), Event(2UL, "unsubscribed")],
                NextSequenceNumber = 3UL,
                TailSequenceNumber = 2UL
            }
        },
        frames => frames.Count >= 2);

    [Fact] void should_append_only_the_allowed_type() => _inbox.Appended.ToArray().ShouldEqual([("customer-store", 0UL)]);
}
