// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.Signing.for_WebhookSigningKeyRingStorageProvider.given;

public class a_storage_provider : Specification
{
    protected WebhookSigningKeyRingStorageProvider _provider;
    protected IWebhookSigningKeysStorage _keysStorage;
    protected IGrainState<WebhookSigningKeyRingState> _grainState;
    protected GrainId _grainId;
    protected WebhookSigningKey[] _storedKeys;

    void Establish()
    {
        _storedKeys =
        [
            new("one", WebhookSigningAlgorithm.Ed25519, WebhookSigningKeyStatus.Active, "whpk_a", "enc:a", null, DateTimeOffset.FromUnixTimeSeconds(100), null, null),
            new("two", WebhookSigningAlgorithm.Ed25519, WebhookSigningKeyStatus.Overlap, "whpk_b", "enc:b", null, DateTimeOffset.FromUnixTimeSeconds(90), DateTimeOffset.FromUnixTimeSeconds(200), null)
        ];
        _keysStorage = Substitute.For<IWebhookSigningKeysStorage>();
        _keysStorage.GetAll().Returns(_storedKeys);
        var storage = Substitute.For<IStorage>();
        storage.GetEventStore("the-store").WebhookSigningKeys.Returns(_keysStorage);
        _provider = new(storage);
        _grainId = GrainId.Create("webhook-signing-key-ring", "the-store");
        _grainState = Substitute.For<IGrainState<WebhookSigningKeyRingState>>();
        _grainState.State = new();
    }
}
