// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_RotateWebhookSigningKey.when_rotating;

public class again_during_an_open_overlap : Signing.for_WebhookSigningKeyRing.given.a_signing_key_ring
{
    IGrainFactory _factory;
    WebhookSigningKeyId _originalId;

    async Task Establish()
    {
        await CreateRing();
        _originalId = (await _ring.GetSigningKeys()).Single().KeyId;
        _factory = Substitute.For<IGrainFactory>();
        _factory.GetGrain<IWebhookSigningKeyRing>("the-store").Returns(_ring);
        await new RotateWebhookSigningKey("the-store").Handle(_factory);
    }

    Task Because() => new RotateWebhookSigningKey("the-store").Handle(_factory);

    [Fact] void should_keep_one_active_key() => State.Keys.Count(key => key.Status == WebhookSigningKeyStatus.Active).ShouldEqual(1);
    [Fact] void should_keep_both_open_overlaps() => State.Keys.Count(key => key.Status == WebhookSigningKeyStatus.Overlap).ShouldEqual(2);
    [Fact] void should_not_discard_the_earlier_overlap() => State.Keys.Single(key => key.KeyId == _originalId).Status.ShouldEqual(WebhookSigningKeyStatus.Overlap);
}
