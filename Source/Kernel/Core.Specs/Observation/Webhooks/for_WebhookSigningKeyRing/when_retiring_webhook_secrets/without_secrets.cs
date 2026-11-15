// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.Signing.for_WebhookSigningKeyRing.when_retiring_webhook_secrets;

public class without_secrets : given.a_signing_key_ring
{
    async Task Establish() => await CreateRing();

    async Task Because() => await _ring.RetireWebhookSecrets("webhook");

    [Fact] void should_not_append_events() => _appended.ShouldBeEmpty();
    [Fact] void should_not_generate() => State.Keys.ShouldBeEmpty();
}
