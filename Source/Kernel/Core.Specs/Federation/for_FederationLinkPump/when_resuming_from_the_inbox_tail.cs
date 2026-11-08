// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationLinkPump;

public class when_resuming_from_the_inbox_tail : given.a_link_pump
{
    TimeSpan? _delay;

    void Establish()
    {
        _inbox.Resume = new("remote", 41);
        _pull.Enqueue(given.FakePull.Batch("remote", 44, 43, given.FakePull.Event(42), given.FakePull.Event(43)));
    }

    async Task Because() => _delay = await _pump.Step(default);

    [Fact] void should_request_from_the_sequence_number_after_the_last_forwarded_one() => _pull.Requests[0].FromSequenceNumber.ShouldEqual(42UL);
    [Fact] void should_request_the_remote_event_store_and_namespace() => (_pull.Requests[0].EventStore, _pull.Requests[0].Namespace).ShouldEqual(("remote", "RemoteNamespace"));
    [Fact] void should_append_the_pulled_events() => _inbox.Appended.Select(_ => _.SequenceNumber).ShouldEqual([42UL, 43UL]);
    [Fact] void should_record_the_origin_of_the_appended_events() => _inbox.Appended.TrueForAll(_ => _.Origin == "remote").ShouldBeTrue();
    [Fact] void should_go_again_immediately_because_the_batch_was_not_empty() => _delay.ShouldEqual(TimeSpan.Zero);
    [Fact] void should_be_streaming() => _status.GetBinding(BindingId).State.ShouldEqual(FederationBindingStates.Streaming);
}
