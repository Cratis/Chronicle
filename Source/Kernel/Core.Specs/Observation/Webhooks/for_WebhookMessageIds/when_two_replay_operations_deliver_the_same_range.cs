// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookMessageIds;

public class when_two_replay_operations_deliver_the_same_range : given.an_identity
{
    string _first;
    string _second;

    void Because()
    {
        _first = WebhookMessageIds.For(Create(epoch: _epoch));
        _second = WebhookMessageIds.For(Create(epoch: _otherEpoch));
    }

    [Fact] void should_differ_between_the_operations() => _first.ShouldNotEqual(_second);
}
