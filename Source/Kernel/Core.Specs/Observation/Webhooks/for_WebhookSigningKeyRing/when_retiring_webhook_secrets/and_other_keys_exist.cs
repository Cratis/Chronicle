// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.Signing.for_WebhookSigningKeyRing.when_retiring_webhook_secrets;

public class and_other_keys_exist : given.a_signing_key_ring
{
    WebhookSigningKey[] _otherKeys;

    async Task Establish()
    {
        _otherKeys = [Key("store", WebhookSigningKeyStatus.Active), Secret("other", "other-webhook", WebhookSigningKeyStatus.Active), Secret("old-other", "other-webhook", WebhookSigningKeyStatus.Overlap, Now.AddHours(1))];
        await CreateRing([.. _otherKeys, Secret("active", "webhook", WebhookSigningKeyStatus.Active)]);
        await _ring.RetireWebhookSecrets("webhook");
        _appended.Clear();
    }

    async Task Because() => await _ring.RetireWebhookSecrets("webhook");

    [Fact] void should_be_idempotent() => _appended.ShouldBeEmpty();
    [Fact] void should_leave_other_keys_unchanged() => _otherKeys.All(original => State.Keys.Single(key => key.KeyId == original.KeyId) == original).ShouldBeTrue();
    [Fact] void should_leave_this_webhooks_secret_erased() => (State.Keys.Single(key => key.KeyId.Value == "active").EncryptedSecret == EncryptedWebhookSigningSecret.Empty).ShouldBeTrue();
}
