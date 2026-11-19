// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookSigningPublicKey.when_getting_keys;

public class with_private_and_unrelated_state : given.a_public_key_query
{
    void Establish() => _keys =
    [
        Key("active", WebhookSigningKeyStatus.Active),
        Key("old", WebhookSigningKeyStatus.Overlap, DateTimeOffset.UtcNow.AddDays(1)),
        Key("expired", WebhookSigningKeyStatus.Overlap, DateTimeOffset.UtcNow.AddDays(-1)),
        Key("retired", WebhookSigningKeyStatus.Retired),
        Key("hmac", WebhookSigningKeyStatus.Active) with { Algorithm = WebhookSigningAlgorithm.HmacSha256, PublicKey = string.Empty },
        Key("scoped", WebhookSigningKeyStatus.Active) with { Webhook = "receiver" }
    ];

    Task Because() => Query();

    [Fact] void should_return_only_valid_store_wide_public_keys() => _result.Select(key => key.KeyId).ShouldEqual(new[] { "active", "old" });
    [Fact] void should_return_public_verification_material() => _result[0].PublicKey.ShouldEqual(_keys[0].PublicKey);
    [Fact] void should_include_overlap_expiry() => _result[1].OverlapEndsAt.ShouldEqual(_keys[1].OverlapEndsAt);
    [Fact] void should_exclude_ciphertext_from_serialization() => _serialized.Contains("encrypted-", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_exclude_private_key_prefixes() => _serialized.Contains("whsk_", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_exclude_hmac_secret_prefixes() => _serialized.Contains("whsec_", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_have_a_private_material_property() => _result[0].GetType().GetProperties().Select(property => property.Name).ShouldContainOnly("KeyId", "Algorithm", "PublicKey", "Status", "CreatedAt", "OverlapEndsAt");
}
