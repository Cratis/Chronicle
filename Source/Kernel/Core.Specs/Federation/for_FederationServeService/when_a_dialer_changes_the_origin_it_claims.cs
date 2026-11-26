// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Federation;

namespace Cratis.Chronicle.Federation.for_FederationServeService;

public class when_a_dialer_changes_the_origin_it_claims : given.a_serve_service
{
    List<ServePullRequest> _frames;

    async Task Establish()
    {
        await SetPolicy("acme", FederationDirection.AcceptorInbound);
        _presented = await Issued("acme");
    }

    async Task Because() => _frames = await Dial(
        request => Answer(request, request.RequestId == 1UL ? "customer-store" : "another-store", request.FromSequenceNumber),
        frames => frames.Count >= 4);

    [Fact] void should_deny_the_dialer() => _frames[^1].Status.ShouldEqual(FederatedEventBatchStatus.Denied);
    [Fact] void should_append_every_event_under_the_authenticated_origin() => _inbox.Appended.TrueForAll(_ => _.Origin == "hosted-acme").ShouldBeTrue();
    [Fact] void should_not_append_what_came_with_the_changed_claim() => _inbox.Appended.Count.ShouldEqual(1);
}
