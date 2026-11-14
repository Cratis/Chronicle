// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Federation.for_FederationPullService.when_pulling;

public class and_a_hosted_peer_is_not_admitted : given.a_pull_service
{
    FederatedEventBatch _batch;

    void Establish()
    {
        RegisterEventType("order-placed", EventTypeVisibility.Public);
        Append("order-placed", 0);
        _admission.Admit(Arg.Any<X509Certificate2?>()).Returns(FederationAdmission.Refused(FederationAdmissionRefusal.Suspended));
    }

    async Task Because() => _batch = await Pull();

    [Fact] void should_be_denied() => _batch.Status.ShouldEqual(FederatedEventBatchStatus.Denied);
    [Fact] void should_return_no_events() => _batch.Events.ShouldBeEmpty();
}
