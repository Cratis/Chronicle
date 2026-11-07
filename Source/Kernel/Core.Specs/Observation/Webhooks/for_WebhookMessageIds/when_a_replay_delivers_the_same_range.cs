// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookMessageIds;

public class when_a_replay_delivers_the_same_range : given.an_identity
{
    string _original;
    string _replayed;

    void Because()
    {
        _original = WebhookMessageIds.For(_identity);
        _replayed = WebhookMessageIds.For(Create(epoch: _epoch));
    }

    [Fact] void should_differ_from_the_original_delivery() => _replayed.ShouldNotEqual(_original);
}
