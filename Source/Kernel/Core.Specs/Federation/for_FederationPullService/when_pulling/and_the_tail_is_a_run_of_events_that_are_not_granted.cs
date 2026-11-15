// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Federation.for_FederationPullService.when_pulling;

public class and_the_tail_is_a_run_of_events_that_are_not_granted : given.a_pull_service
{
    FederatedEventBatch _batch;

    void Establish()
    {
        _grants.Add(GrantForCaller());
        RegisterEventType("private-type", EventTypeVisibility.Private);
        for (ulong sequenceNumber = 0; sequenceNumber < 8; sequenceNumber++)
        {
            Append("private-type", sequenceNumber);
        }
    }

    async Task Because() => _batch = await Pull();

    [Fact] void should_be_ok() => _batch.Status.ShouldEqual(FederatedEventBatchStatus.Ok);
    [Fact] void should_return_no_events() => _batch.Events.ShouldBeEmpty();
    [Fact] void should_advance_past_the_run_so_the_consumer_never_stalls() => _batch.NextSequenceNumber.ShouldEqual(8UL);
    [Fact] void should_report_the_tail() => _batch.TailSequenceNumber.ShouldEqual(7UL);
}
