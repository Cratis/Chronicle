// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.Signing.for_WebhookSigningKeyRing.when_getting_webhook_secrets;

public class and_none_exists : given.a_signing_key_ring
{
    IEnumerable<WebhookSigningKey> _result;

    async Task Establish() => await CreateRing(Key("store", WebhookSigningKeyStatus.Active), Secret("other", "other-webhook", WebhookSigningKeyStatus.Active));

    async Task Because() => _result = await _ring.GetWebhookSecrets("webhook");

    [Fact] void should_return_no_secrets() => _result.ShouldBeEmpty();
    [Fact] void should_not_generate_on_demand() => _appended.ShouldBeEmpty();
    [Fact] void should_not_encrypt() => _encrypted.ShouldBeEmpty();
}
