// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookSigningPublicKey.when_getting_keys;

public class with_a_malformed_public_key : given.a_public_key_query
{
    void Establish() => _keys = [_keys[0] with { PublicKey = "whsk_must-not-escape" }];
    async Task Because() => _error = await Catch.Exception(Query);

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<WebhookSigningUnavailable>();
    [Fact] void should_not_retain_private_material() => _error!.ToString().Contains("whsk_", StringComparison.Ordinal).ShouldBeFalse();
}
