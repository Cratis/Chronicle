// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.Signing.for_WebhookSigningKeyRing.when_rotating_a_webhook_secret;

public class and_the_store_has_a_signing_key : given.a_signing_key_ring
{
    WebhookSigningKey _storeKey;

    async Task Establish()
    {
        _storeKey = Key("store", WebhookSigningKeyStatus.Active);
        await CreateRing(_storeKey);
    }

    async Task Because() => await _ring.RotateWebhookSecret("webhook", WebhookSigningKeyOverlap.Default);

    [Fact] void should_leave_the_store_key_unchanged() => (State.Keys.Single(key => key.KeyId == _storeKey.KeyId) == _storeKey).ShouldBeTrue();
    [Fact] void should_not_append_for_the_store_key() => _appended.Exists(entry => entry.EventSource.Value == _storeKey.KeyId.Value).ShouldBeFalse();
}
