// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.Signing.for_WebhookSigningKeyRing.when_getting_webhook_secrets;

public class and_an_overlap_has_expired : given.a_signing_key_ring
{
    IEnumerable<WebhookSigningKey> _result;

    async Task Establish() => await CreateRing(Secret("expired", "webhook", WebhookSigningKeyStatus.Overlap, Now), Secret("active", "webhook", WebhookSigningKeyStatus.Active));

    async Task Because() => _result = await _ring.GetWebhookSecrets("webhook");

    [Fact] void should_not_return_the_expired_secret() => _result.Select(key => key.KeyId.Value).ShouldEqual(["active"]);
    [Fact] void should_append_retired() => _appended.Select(entry => entry.Event).ShouldEqual([new WebhookSigningKeyRetired("expired")]);
    [Fact] void should_retire_at_the_current_time() => (State.Keys.Single(key => key.KeyId.Value == "expired").Status, State.Keys.Single(key => key.KeyId.Value == "expired").RetiredAt).ShouldEqual((WebhookSigningKeyStatus.Retired, (DateTimeOffset?)Now));
    [Fact] void should_erase_expired_ciphertext() => (State.Keys.Single(key => key.KeyId.Value == "expired").EncryptedSecret == EncryptedWebhookSigningSecret.Empty).ShouldBeTrue();
}
