// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Federation.for_FederationPullService.when_pulling;

public class and_the_peer_is_pinned_and_hosted : given.a_pull_service
{
    FederatedEventBatch _batch;
    FederatedEventBatch _pinnedStoreDenied;

    void Establish()
    {
        _grants.Add(GrantForCaller(allPublic: false, eventTypes: "order-placed"));
        RegisterEventType("order-placed", EventTypeVisibility.Public);
        RegisterEventType("order-shipped", EventTypeVisibility.Public);
        Append("order-placed", 0);
        Append("order-shipped", 1);

        // A hosted policy that would allow everything, at another target, must never widen or replace the pinned grant.
        AdmitHostedPeer(allPublic: true, eventStore: "elsewhere");
    }

    async Task Because()
    {
        _batch = await Pull();
        _pinnedStoreDenied = await Pull(eventStore: "elsewhere");
    }

    [Fact] void should_serve_by_the_pinned_grant() => _batch.Events.Select(_ => _.SequenceNumber).ShouldContainOnly(0UL);
    [Fact] void should_not_consult_the_hosted_admission() => _admission.DidNotReceive().Admit(Arg.Any<X509Certificate2?>());
    [Fact] void should_still_deny_a_target_the_pinned_grant_does_not_cover() => _pinnedStoreDenied.Status.ShouldEqual(FederatedEventBatchStatus.Denied);
}
