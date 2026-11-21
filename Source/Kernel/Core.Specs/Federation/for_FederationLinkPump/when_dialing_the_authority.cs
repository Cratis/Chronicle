// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Federation;

namespace Cratis.Chronicle.Federation.for_FederationLinkPump;

public class when_dialing_the_authority : given.a_dialing_link_pump
{
    TimeSpan? _delay;

    void Establish()
    {
        _serve.Script.Add(new ServePullRequest { RequestId = 1, FromSequenceNumber = 0, Limit = 10, EventTypes = ["type-a"] });
        _pull.Enqueue(given.FakePull.Batch("remote", 2, 1, given.FakePull.Event(0), given.FakePull.Event(1)));
    }

    async Task Because()
    {
        _delay = await _dialPump.Step(CancellationToken.None);
        await _serve.WhenReceived(1);
    }

    [Fact] void should_connect_with_the_enrolled_certificate() => _dialClients.Created.ShouldEqual(1);
    [Fact] void should_not_use_a_configured_certificate() => _clients.Created.ShouldEqual(0);
    [Fact] void should_pull_from_the_authority_without_naming_its_target() => _pull.Requests.Single().EventStore.ShouldEqual(string.Empty);
    [Fact] void should_forward_what_it_pulled() => _inbox.Appended.Count.ShouldEqual(2);
    [Fact] void should_open_the_serve_call_on_the_same_connection() => _serve.Calls.ShouldEqual(1);
    [Fact] void should_answer_the_requests_of_the_authority() => _serve.Received.Single().RequestId.ShouldEqual(1UL);
    [Fact] void should_have_used_a_single_connection() => _dialClients.Created.ShouldEqual(1);
    [Fact] void should_go_again_at_once_because_there_was_more() => _delay.ShouldEqual(TimeSpan.Zero);
}
