// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Federation;

namespace Cratis.Chronicle.Federation.for_FederationServeService;

public class when_a_dialer_serves_its_events : given.a_serve_service
{
    List<ServePullRequest> _frames;

    async Task Establish()
    {
        await SetPolicy("acme", FederationDirection.AcceptorInbound, false, "type-a");
        _presented = await Issued("acme");
    }

    async Task Because()
    {
        var batches = new Queue<ulong[]>([[0UL, 1UL], [2UL], []]);
        _frames = await Dial(
            request => Answer(request, "customer-store", batches.Count > 0 ? batches.Dequeue() : []),
            frames => frames.Count >= 4);
    }

    [Fact] void should_start_from_the_beginning() => _frames[0].FromSequenceNumber.ShouldEqual(0UL);
    [Fact] void should_move_the_cursor_past_what_arrived() => _frames.Select(_ => _.FromSequenceNumber).Take(3).ToArray().ShouldEqual([0UL, 2UL, 3UL]);
    [Fact] void should_number_the_requests() => _frames.Select(_ => _.RequestId).Take(3).ToArray().ShouldEqual([1UL, 2UL, 3UL]);
    [Fact] void should_ask_only_for_what_is_subscribed_and_allowed() => _frames[0].EventTypes.ToArray().ShouldEqual(["type-a"]);
    [Fact] void should_append_every_event_in_order() => _inbox.Appended.ToArray().ShouldEqual([("customer-store", 0UL), ("customer-store", 1UL), ("customer-store", 2UL)]);
    [Fact] void should_only_append_to_the_target_of_the_policy() => _inbox.Targets.Distinct().ToArray().ShouldEqual([(TargetStore, TargetNamespace)]);
    [Fact] void should_not_send_a_denied_frame() => _frames.Exists(_ => _.Status == FederatedEventBatchStatus.Denied).ShouldBeFalse();
}
