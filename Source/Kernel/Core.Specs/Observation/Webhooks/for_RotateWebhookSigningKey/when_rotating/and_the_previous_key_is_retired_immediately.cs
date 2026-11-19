// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Observation.Webhooks.Signing;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Observation.Webhooks.for_RotateWebhookSigningKey.when_rotating;

public class and_the_previous_key_is_retired_immediately : Signing.for_WebhookSigningKeyRing.given.a_signing_key_ring
{
    IGrainFactory _factory;
    IStorage _storage;
    WebhookSigningKeyId _originalId;

    async Task Establish()
    {
        await CreateRing();
        _originalId = (await _ring.GetSigningKeys()).Single().KeyId;
        _factory = Substitute.For<IGrainFactory>();
        _storage = Substitute.For<IStorage>();
        _storage.HasEventStore("the-store").Returns(true);
        _factory.GetGrain<IWebhookSigningKeyRing>("the-store").Returns(_ring);
    }

    Task Because() => new RotateWebhookSigningKey("the-store", true, 0).Handle(_factory, _storage);

    [Fact] void should_retire_the_previous_key() => State.Keys.Single(key => key.KeyId == _originalId).Status.ShouldEqual(WebhookSigningKeyStatus.Retired);
    [Fact] void should_erase_its_ciphertext() => State.Keys.Single(key => key.KeyId == _originalId).EncryptedSecret.ShouldEqual(EncryptedWebhookSigningSecret.Empty);
    [Fact] void should_keep_one_active_key() => State.Keys.Count(key => key.Status == WebhookSigningKeyStatus.Active).ShouldEqual(1);
}
