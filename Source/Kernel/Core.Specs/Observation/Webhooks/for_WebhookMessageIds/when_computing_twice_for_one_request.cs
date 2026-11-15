// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookMessageIds;

public class when_computing_twice_for_one_request : given.an_identity
{
    string _first;
    string _second;
    string _firstReplay;
    string _secondReplay;

    void Because()
    {
        _first = WebhookMessageIds.For(_identity);
        _second = WebhookMessageIds.For(Create());
        _firstReplay = WebhookMessageIds.For(Create(epoch: _epoch));
        _secondReplay = WebhookMessageIds.For(Create(epoch: _epoch));
    }

    [Fact] void should_give_the_same_id_for_a_retry() => _first.ShouldEqual(_second);
    [Fact] void should_give_the_same_id_for_a_retry_of_a_replay_delivery() => _firstReplay.ShouldEqual(_secondReplay);
}
