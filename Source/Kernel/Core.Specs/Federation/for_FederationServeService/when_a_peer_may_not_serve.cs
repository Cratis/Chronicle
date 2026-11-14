// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Federation;

namespace Cratis.Chronicle.Federation.for_FederationServeService;

public class when_a_peer_may_not_serve : given.a_serve_service
{
    List<ServePullRequest> _withoutInbound;
    List<ServePullRequest> _withoutCertificate;
    List<ServePullRequest> _suspended;

    async Task Because()
    {
        // Only the direction the peer pulls in is allowed: serving is a direction of its own.
        await SetPolicy("acme", FederationDirection.AcceptorOutbound);
        _presented = await Issued("acme");
        _withoutInbound = await Dial(request => Answer(request, "customer-store", 0UL), _ => false);

        _presented = null!;
        _withoutCertificate = await Dial(request => Answer(request, "customer-store", 0UL), _ => false);

        await SetPolicy("acme", FederationDirection.AcceptorInbound);
        _presented = await Issued("acme", "other");
        await _policies.Suspend("acme", "operator");
        _suspended = await Dial(request => Answer(request, "customer-store", 0UL), _ => false);
    }

    [Fact] void should_refuse_a_peer_without_an_inbound_entry() => _withoutInbound.Select(_ => _.Status).ToArray().ShouldEqual([FederatedEventBatchStatus.Denied]);
    [Fact] void should_refuse_a_peer_without_a_certificate() => _withoutCertificate.Select(_ => _.Status).ToArray().ShouldEqual([FederatedEventBatchStatus.Denied]);
    [Fact] void should_refuse_a_suspended_subject() => _suspended.Select(_ => _.Status).ToArray().ShouldEqual([FederatedEventBatchStatus.Denied]);
    [Fact] void should_append_nothing() => _inbox.Appended.ShouldBeEmpty();
}
