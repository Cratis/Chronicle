// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationServeService;

public class when_a_dialer_delivers_the_same_events_again : given.a_serve_service
{
    async Task Establish()
    {
        await SetPolicy("acme", FederationDirection.AcceptorInbound);
        _presented = await Issued("acme");
    }

    async Task Because() => await Dial(request => Answer(request, "customer-store", 0UL, 1UL), frames => frames.Count >= 3);

    [Fact] void should_append_each_event_once() => _inbox.Appended.ToArray().ShouldEqual([("customer-store", 0UL), ("customer-store", 1UL)]);
    [Fact] void should_count_the_repeats_as_duplicates() => _consumerMetrics.Received().EventSkipped(Arg.Any<string>(), FederationSkipReasons.Duplicate);
}
