// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.Signing.for_WebhookSigningKeyRing.when_getting_signing_keys;

public class and_an_active_key_exists : given.a_signing_key_ring
{
    WebhookSigningKey _active;
    IEnumerable<WebhookSigningKey> _result;

    async Task Establish()
    {
        _active = Key("active", WebhookSigningKeyStatus.Active);
        await CreateRing(_active);
    }

    async Task Because() => _result = await _ring.GetSigningKeys();

    [Fact] void should_return_the_existing_key() => _result.ShouldContainOnly(_active);
    [Fact] void should_append_nothing() => _appended.ShouldBeEmpty();
}
