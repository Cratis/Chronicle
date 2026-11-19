// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookSigningPublicKey.when_getting_keys;

public class with_an_open_overlap : given.a_public_key_query
{
    byte[] _publishedOldKey;

    void Establish() => _keys = [Key("active", WebhookSigningKeyStatus.Active), Key("old", WebhookSigningKeyStatus.Overlap, DateTimeOffset.UtcNow.AddDays(1))];

    async Task Because()
    {
        await Sign();
        await Query();
        WebhookSigningKeys.TryParsePublicKey(_result[1].PublicKey, out _publishedOldKey).ShouldBeTrue();
    }

    [Fact] void should_publish_the_old_verification_key() => WebhookSigningKeys.TryParsePublicKey(_result[1].PublicKey, out _).ShouldBeTrue();
    [Fact] void should_allow_verification_with_only_the_old_key() => WebhookSignatureVerification.Verify("message", 1000, Body, _signature.Signature, [], [_publishedOldKey], "unknown-advisory-id").ShouldBeTrue();
}
