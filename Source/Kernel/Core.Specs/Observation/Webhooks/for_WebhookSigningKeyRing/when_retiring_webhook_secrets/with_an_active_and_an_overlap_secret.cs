// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.Signing.for_WebhookSigningKeyRing.when_retiring_webhook_secrets;

public class with_an_active_and_an_overlap_secret : given.a_signing_key_ring
{
    async Task Establish() => await CreateRing(Secret("active", "webhook", WebhookSigningKeyStatus.Active), Secret("overlap", "webhook", WebhookSigningKeyStatus.Overlap, Now.AddHours(2)));

    async Task Because() => await _ring.RetireWebhookSecrets("webhook");

    [Fact] void should_append_one_retirement_per_secret() => _appended.Select(entry => entry.Event).ShouldEqual([new WebhookSigningKeyRetired("active"), new WebhookSigningKeyRetired("overlap")]);
    [Fact] void should_retire_all_secrets_now() => State.Keys.All(key => key.Status == WebhookSigningKeyStatus.Retired && key.RetiredAt == Now).ShouldBeTrue();
    [Fact] void should_erase_all_ciphertext() => State.Keys.All(key => key.EncryptedSecret == EncryptedWebhookSigningSecret.Empty).ShouldBeTrue();
}
