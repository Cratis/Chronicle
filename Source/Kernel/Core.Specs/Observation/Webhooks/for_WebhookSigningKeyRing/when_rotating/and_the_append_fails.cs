// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.Signing.for_WebhookSigningKeyRing.when_rotating;

public class and_the_append_fails : given.a_signing_key_ring
{
    WebhookSigningKey _active;
    Exception _error;

    async Task Establish()
    {
        _active = Key("active", WebhookSigningKeyStatus.Active);
        await CreateRing(_active);
        _appendSucceeds = false;
    }

    async Task Because() => _error = await Catch.Exception(() => _ring.Rotate(WebhookSigningKeyOverlap.Default));

    [Fact] void should_throw_append_failed() => _error.ShouldBeOfExactType<WebhookSigningKeyRingAppendFailed>();
    [Fact] void should_leave_the_state_untouched() => State.Keys.ShouldContainOnly(_active);
    [Fact] void should_not_put_key_material_in_the_message() => _error.Message.ShouldNotContain("whsk_");
}
