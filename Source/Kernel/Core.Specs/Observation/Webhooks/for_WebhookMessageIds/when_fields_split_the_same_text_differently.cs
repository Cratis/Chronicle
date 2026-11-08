// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookMessageIds;

public class when_fields_split_the_same_text_differently : given.an_identity
{
    string _first;
    string _second;

    void Because()
    {
        _first = WebhookMessageIds.For(Create(eventStore: "ab", @namespace: "c"));
        _second = WebhookMessageIds.For(Create(eventStore: "a", @namespace: "bc"));
    }

    [Fact] void should_not_collide() => _first.ShouldNotEqual(_second);
}
