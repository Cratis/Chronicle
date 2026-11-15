// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.Signing.for_WebhookSigningKeyRing.when_rotating_a_webhook_secret;

public class and_another_webhook_has_a_secret : given.a_signing_key_ring
{
    WebhookSigningKey _other;

    async Task Establish()
    {
        _other = Secret("other", "other-webhook", WebhookSigningKeyStatus.Active);
        await CreateRing(_other, Key("store", WebhookSigningKeyStatus.Active), Secret("active", "webhook", WebhookSigningKeyStatus.Active));
    }

    async Task Because() => await _ring.RotateWebhookSecret("webhook", WebhookSigningKeyOverlap.Default);

    [Fact] void should_leave_the_other_secret_unchanged() => (State.Keys.Single(key => key.KeyId == _other.KeyId) == _other).ShouldBeTrue();
    [Fact] void should_leave_the_store_key_unchanged() => (State.Keys.Single(key => key.KeyId.Value == "store") == Key("store", WebhookSigningKeyStatus.Active)).ShouldBeTrue();
    [Fact] void should_append_only_for_this_webhook() => _appended.Exists(entry => entry.EventSource.Value == "other" || entry.EventSource.Value == "store").ShouldBeFalse();
}
