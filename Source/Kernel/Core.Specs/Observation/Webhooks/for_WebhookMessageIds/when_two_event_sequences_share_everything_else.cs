// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookMessageIds;

public class when_two_event_sequences_share_everything_else : given.an_identity
{
    string _first;
    string _second;

    void Because()
    {
        _first = WebhookMessageIds.For(Create(sequence: "sequence-a"));
        _second = WebhookMessageIds.For(Create(sequence: "sequence-b"));
    }

    [Fact] void should_not_collide() => _first.ShouldNotEqual(_second);
}
