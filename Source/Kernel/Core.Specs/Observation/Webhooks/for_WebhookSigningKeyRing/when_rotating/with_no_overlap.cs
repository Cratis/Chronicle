// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.Signing.for_WebhookSigningKeyRing.when_rotating;

public class with_no_overlap : given.a_signing_key_ring
{
    async Task Establish() => await CreateRing(Key("active", WebhookSigningKeyStatus.Active));

    async Task Because() => await _ring.Rotate(WebhookSigningKeyOverlap.None);

    [Fact] void should_append_superseded_retired_then_generated() => _appended.Select(_ => _.Event.GetType()).ShouldEqual([typeof(WebhookSigningKeySuperseded), typeof(WebhookSigningKeyRetired), typeof(WebhookSigningKeyGenerated)]);
    [Fact] void should_retire_the_old_key() => State.Keys.Single(_ => _.KeyId.Value == "active").Status.ShouldEqual(WebhookSigningKeyStatus.Retired);
    [Fact] void should_erase_its_secret() => State.Keys.Single(_ => _.KeyId.Value == "active").EncryptedSecret.ShouldEqual(EncryptedWebhookSigningSecret.Empty);
}
