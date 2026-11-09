// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationPullService.when_pulling;

public class and_the_outbox_is_empty : given.a_pull_service
{
    FederatedEventBatch _batch;

    void Establish() => _grants.Add(GrantForCaller());

    async Task Because() => _batch = await Pull(from: 7);

    [Fact] void should_be_ok() => _batch.Status.ShouldEqual(FederatedEventBatchStatus.Ok);
    [Fact] void should_return_no_events() => _batch.Events.ShouldBeEmpty();
    [Fact] void should_stay_where_the_consumer_was() => _batch.NextSequenceNumber.ShouldEqual(7UL);
}
