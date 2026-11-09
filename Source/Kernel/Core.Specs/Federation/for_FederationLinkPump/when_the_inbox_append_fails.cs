// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationLinkPump;

public class when_the_inbox_append_fails : given.a_link_pump
{
    TimeSpan? _firstDelay;

    void Establish()
    {
        _inbox.FailuresRemaining = 1;
        _pull.Enqueue(given.FakePull.Batch("remote", 3, 2, given.FakePull.Event(1), given.FakePull.Event(2)));
        _pull.Enqueue(given.FakePull.Batch("remote", 3, 2, given.FakePull.Event(1), given.FakePull.Event(2)));
    }

    async Task Because()
    {
        _firstDelay = await _pump.Step(default);
        FailedStatus = _status.GetBinding(BindingId);
        await _pump.Step(default);
    }

    FederationBindingStatus FailedStatus { get; set; }

    [Fact] void should_back_off() => _firstDelay.ShouldEqual(_timing.InitialBackoff);
    [Fact] void should_record_the_failure() => FailedStatus.State.ShouldEqual(FederationBindingStates.Backoff);
    [Fact] void should_keep_the_error_in_the_status() => FailedStatus.LastError.ShouldEqual("Inbox append error");
    [Fact] void should_not_advance_the_cursor() => _pull.Requests[1].FromSequenceNumber.ShouldEqual(0UL);
    [Fact] void should_append_the_events_on_retry_without_mistaking_them_for_duplicates() => _inbox.Appended.Select(_ => _.SequenceNumber).ShouldEqual([1UL, 2UL]);
}
