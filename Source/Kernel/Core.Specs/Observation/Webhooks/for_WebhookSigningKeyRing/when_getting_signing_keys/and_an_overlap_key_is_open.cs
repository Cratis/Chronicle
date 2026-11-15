// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.Signing.for_WebhookSigningKeyRing.when_getting_signing_keys;

public class and_an_overlap_key_is_open : given.a_signing_key_ring
{
    WebhookSigningKey _active;
    WebhookSigningKey _overlap;
    WebhookSigningKey[] _result;

    async Task Establish()
    {
        _overlap = Key("overlap", WebhookSigningKeyStatus.Overlap, Now.AddHours(1));
        _active = Key("active", WebhookSigningKeyStatus.Active);
        await CreateRing(_overlap, _active);
    }

    async Task Because() => _result = [.. await _ring.GetSigningKeys()];

    [Fact] void should_return_the_active_key_first() => _result[0].ShouldEqual(_active);
    [Fact] void should_return_the_overlap_key_second() => _result[1].ShouldEqual(_overlap);
    [Fact] void should_append_nothing() => _appended.ShouldBeEmpty();
}
