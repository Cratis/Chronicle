// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Federation;

namespace Cratis.Chronicle.Federation.for_FederationLinkPump;

public class when_a_dialer_only_serves : given.a_dialing_link_pump
{
    TimeSpan? _delay;

    void Establish()
    {
        _subscriptions.Subscriptions.Clear();
        _serve.Script.Add(new ServePullRequest { RequestId = 1, FromSequenceNumber = 0, Limit = 10, EventTypes = ["type-a"] });
    }

    async Task Because()
    {
        _delay = await _dialPump.Step(CancellationToken.None);
        await _serve.WhenReceived(1);
    }

    [Fact] void should_still_connect() => _dialClients.Created.ShouldEqual(1);
    [Fact] void should_pull_nothing() => _pull.Requests.ShouldBeEmpty();
    [Fact] void should_serve() => _serve.Received.Count.ShouldEqual(1);
    [Fact] void should_check_again_after_the_poll_interval() => _delay.ShouldEqual(_timing.PollInterval);
}
