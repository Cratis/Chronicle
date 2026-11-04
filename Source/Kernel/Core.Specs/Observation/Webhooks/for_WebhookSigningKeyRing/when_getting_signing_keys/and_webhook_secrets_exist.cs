// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.Signing.for_WebhookSigningKeyRing.when_getting_signing_keys;

public class and_webhook_secrets_exist : given.a_signing_key_ring
{
    IEnumerable<WebhookSigningKey> _result;

    async Task Establish() => await CreateRing(
        Secret("secret", "webhook", WebhookSigningKeyStatus.Active),
        Secret("old-secret", "webhook", WebhookSigningKeyStatus.Overlap, Now.AddHours(1)),
        Key("old-key", WebhookSigningKeyStatus.Overlap, Now.AddHours(1)),
        Key("key", WebhookSigningKeyStatus.Active));

    async Task Because() => _result = await _ring.GetSigningKeys();

    [Fact] void should_return_only_store_wide_keys_active_first() => _result.Select(key => key.KeyId.Value).ShouldEqual(["key", "old-key"]);
    [Fact] void should_not_append_events() => _appended.ShouldBeEmpty();
}
