// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Federation;

namespace Cratis.Chronicle.Federation.for_FederationServeService;

public class when_the_inbound_direction_is_revoked_while_serving : given.a_serve_service
{
    List<ServePullRequest> _frames;
    FederationAdmission _outbound;

    async Task Establish()
    {
        await SetPolicy("acme", FederationDirection.AcceptorInbound);
        await SetPolicy("acme", FederationDirection.AcceptorOutbound);
        _presented = await Issued("acme");
    }

    async Task Because()
    {
        _frames = await Dial(
            request =>
            {
                if (request.RequestId == 1UL)
                {
                    _policies.RevokeDirection("acme", FederationDirection.AcceptorInbound, "operator").GetAwaiter().GetResult();
                }

                return Answer(request, "customer-store", request.FromSequenceNumber);
            },
            frames => frames.Count >= 5);
        _outbound = await Admit(_presented);
    }

    [Fact] void should_serve_the_first_request() => _frames[0].Status.ShouldNotEqual(FederatedEventBatchStatus.Denied);
    [Fact] void should_end_with_a_denied_frame() => _frames[^1].Status.ShouldEqual(FederatedEventBatchStatus.Denied);
    [Fact] void should_stop_asking_for_events() => _frames.Count.ShouldEqual(2);
    [Fact] void should_not_append_what_arrived_after_the_revocation() => _inbox.Appended.ShouldBeEmpty();
    [Fact] void should_keep_admitting_the_peer_for_the_other_direction() => _outbound.Policy.EntryFor(FederationDirection.AcceptorOutbound).ShouldNotBeNull();
}
