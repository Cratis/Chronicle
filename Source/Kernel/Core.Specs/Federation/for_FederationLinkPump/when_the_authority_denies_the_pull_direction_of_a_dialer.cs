// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Federation;

namespace Cratis.Chronicle.Federation.for_FederationLinkPump;

public class when_the_authority_denies_the_pull_direction_of_a_dialer : given.a_dialing_link_pump
{
    TimeSpan? _delay;

    void Establish()
    {
        _serve.Script.Add(new ServePullRequest { RequestId = 1, FromSequenceNumber = 0, Limit = 10, EventTypes = ["type-a"] });
        _pull.Enqueue(given.FakePull.WithStatus(FederatedEventBatchStatus.Denied));
    }

    async Task Because()
    {
        _delay = await _dialPump.Step(CancellationToken.None);
        await Eventually(() => _serve.Received.Count > 0);
    }

    [Fact] void should_wait_before_pulling_again() => _delay.ShouldEqual(_timing.DeniedBackoff);
    [Fact] void should_keep_the_connection_because_the_other_direction_may_still_be_allowed() => _dialClients.Closed.ShouldEqual(0);
    [Fact] void should_keep_serving_on_it() => _serve.Received.Count.ShouldEqual(1);
    [Fact] void should_not_reconnect() => _dialClients.Created.ShouldEqual(1);
}
