// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Federation.for_FederationPullService.when_pulling;

public class and_the_grant_covers_all_public_event_types : given.a_pull_service
{
    FederatedEventBatch _batch;

    void Establish()
    {
        _grants.Add(GrantForCaller());
        RegisterEventType("public-type", EventTypeVisibility.Public);
        RegisterEventType("private-type", EventTypeVisibility.Private);
        RegisterEventType("unspecified-type", EventTypeVisibility.Unspecified);
        Append("public-type", 0);
        Append("private-type", 1);
        Append("unspecified-type", 2);
        Append("public-type", 3);
    }

    async Task Because() => _batch = await Pull();

    [Fact] void should_be_ok() => _batch.Status.ShouldEqual(FederatedEventBatchStatus.Ok);
    [Fact] void should_only_deliver_the_public_events() => _batch.Events.Select(_ => _.SequenceNumber).ShouldContainOnly(0UL, 3UL);
    [Fact] void should_report_the_origin_event_store() => _batch.OriginEventStore.ShouldEqual(Store);
    [Fact] void should_advance_past_everything_scanned() => _batch.NextSequenceNumber.ShouldEqual(4UL);
    [Fact] void should_report_the_tail() => _batch.TailSequenceNumber.ShouldEqual(3UL);
    [Fact] void should_record_the_pull() => _status.Received(1).RecordPull("link-a", 2, Arg.Any<long>());
}
