// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.Signing.for_WebhookSigningKeyRing.when_rotating;

public class without_a_key : given.a_signing_key_ring
{
    WebhookSigningKey _result;

    async Task Establish() => await CreateRing();

    async Task Because() => _result = await _ring.Rotate(WebhookSigningKeyOverlap.Default);

    [Fact] void should_append_only_generated() => _appended.Select(_ => _.Event.GetType()).ShouldEqual([typeof(WebhookSigningKeyGenerated)]);
    [Fact] void should_return_an_active_key() => _result.Status.ShouldEqual(WebhookSigningKeyStatus.Active);
}
