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

    [Fact] void should_append_the_events_under_the_authenticated_origin() => _inbox.Appended.ToArray().ShouldEqual([("hosted-acme", 0UL), ("hosted-acme", 1UL)]);
    [Fact] void should_never_use_the_claimed_origin() => _inbox.Appended.Exists(_ => _.Origin == TargetStore).ShouldBeFalse();
}
