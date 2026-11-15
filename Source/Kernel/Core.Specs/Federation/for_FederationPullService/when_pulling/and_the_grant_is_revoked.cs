// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Federation.for_FederationPullService.when_pulling;

public class and_the_grant_is_revoked : given.a_pull_service
{
    FederatedEventBatch _batch;

    void Establish()
    {
        _grants.Add(Grant("link-a", CertificatePin.From(_certificate), revoked: true));
        RegisterEventType("order-placed", EventTypeVisibility.Public);
        Append("order-placed", 0);
    }

    async Task Because() => _batch = await Pull();

    [Fact] void should_be_denied() => _batch.Status.ShouldEqual(FederatedEventBatchStatus.Denied);
    [Fact] void should_return_no_events() => _batch.Events.ShouldBeEmpty();
}
