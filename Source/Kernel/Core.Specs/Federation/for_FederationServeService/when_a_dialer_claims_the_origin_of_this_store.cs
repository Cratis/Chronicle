// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationServeService;

public class when_a_dialer_claims_the_origin_of_this_store : given.a_serve_service
{
    async Task Establish()
    {
        await SetPolicy("acme", FederationDirection.AcceptorInbound);
        _presented = await Issued("acme");
    }

    async Task Because() => await Dial(request => Answer(request, TargetStore, 0UL, 1UL), frames => frames.Count >= 2);

    [Fact] void should_append_nothing() => _inbox.Appended.ShouldBeEmpty();
    [Fact] void should_count_the_events_as_skipped_for_their_origin() => _consumerMetrics.Received().EventSkipped(Arg.Any<string>(), FederationSkipReasons.OwnOrigin);
}
