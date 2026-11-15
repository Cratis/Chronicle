// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.Signing.for_WebhookSigningKeyRing.when_rotating_a_webhook_secret;

public class with_an_active_secret : given.a_signing_key_ring
{
    WebhookSigningKey _result;

    async Task Establish() => await CreateRing(Secret("active", "webhook", WebhookSigningKeyStatus.Active));

    async Task Because() => _result = await _ring.RotateWebhookSecret("webhook", TimeSpan.FromHours(2));

    [Fact] void should_supersede_then_generate() => _appended.Select(entry => entry.Event.GetType()).ShouldEqual([typeof(WebhookSigningKeySuperseded), typeof(WebhookSigningKeyGenerated)]);
    [Fact] void should_set_the_overlap_deadline() => _appended[0].Event.ShouldEqual(new WebhookSigningKeySuperseded("active", Now.AddHours(2)));
    [Fact] void should_keep_the_previous_secret_in_overlap() => (State.Keys.Single(key => key.KeyId.Value == "active").Status, State.Keys.Single(key => key.KeyId.Value == "active").OverlapEndsAt).ShouldEqual((WebhookSigningKeyStatus.Overlap, (DateTimeOffset?)Now.AddHours(2)));
    [Fact] void should_keep_the_previous_ciphertext() => (State.Keys.Single(key => key.KeyId.Value == "active").EncryptedSecret == Secret("active", "webhook", WebhookSigningKeyStatus.Active).EncryptedSecret).ShouldBeTrue();
    [Fact] void should_return_a_new_active_secret() => (_result.Status, _result.KeyId.Value != "active").ShouldEqual((WebhookSigningKeyStatus.Active, true));
}
