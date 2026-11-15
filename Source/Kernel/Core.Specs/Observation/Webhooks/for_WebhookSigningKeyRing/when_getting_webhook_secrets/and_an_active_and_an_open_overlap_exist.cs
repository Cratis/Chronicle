// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.Signing.for_WebhookSigningKeyRing.when_getting_webhook_secrets;

public class and_an_active_and_an_open_overlap_exist : given.a_signing_key_ring
{
    IEnumerable<WebhookSigningKey> _result;

    async Task Establish() => await CreateRing(
        Secret("overlap", "webhook", WebhookSigningKeyStatus.Overlap, Now.AddHours(1)),
        Key("store", WebhookSigningKeyStatus.Active),
        Secret("other", "other-webhook", WebhookSigningKeyStatus.Active),
        Secret("active", "webhook", WebhookSigningKeyStatus.Active),
        Secret("retired", "webhook", WebhookSigningKeyStatus.Retired));

    async Task Because() => _result = await _ring.GetWebhookSecrets("webhook");

    [Fact] void should_return_only_this_webhooks_valid_secrets_active_first() => _result.Select(key => key.KeyId.Value).ShouldEqual(["active", "overlap"]);
    [Fact] void should_return_ciphertext_only() => _result.All(key => key.EncryptedSecret == State.Keys.Single(stored => stored.KeyId == key.KeyId).EncryptedSecret).ShouldBeTrue();
    [Fact] void should_not_append_events() => _appended.ShouldBeEmpty();
}
