// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Federation.for_FederationPullService.when_pulling;

public class and_there_is_no_client_certificate : given.a_pull_service
{
    FederatedEventBatch _batch;

    void Establish()
    {
        _grants.Add(GrantForCaller());
        RegisterEventType("order-placed", EventTypeVisibility.Public);
        Append("order-placed", 0);
        _presented = null;
    }

    async Task Because() => _batch = await Pull();

    [Fact] void should_be_denied() => _batch.Status.ShouldEqual(FederatedEventBatchStatus.Denied);
    [Fact] void should_return_no_events() => _batch.Events.ShouldBeEmpty();
    [Fact] void should_not_reveal_the_event_store() => _batch.OriginEventStore.ShouldEqual(string.Empty);
    [Fact] void should_not_ask_the_registry() => _registry.DidNotReceive().FindActiveGrants(Arg.Any<CertificatePin>());
    [Fact] void should_not_record_a_pull() => _status.DidNotReceive().RecordPull(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<long>());
}
