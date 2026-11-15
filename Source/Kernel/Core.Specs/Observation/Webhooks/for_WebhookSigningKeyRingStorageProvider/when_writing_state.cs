// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.Signing.for_WebhookSigningKeyRingStorageProvider;

public class when_writing_state : given.a_storage_provider
{
    void Establish() => _grainState.State = new() { Keys = _storedKeys };

    Task Because() => _provider.WriteStateAsync("state", _grainId, _grainState);

    [Fact] void should_save_the_first_key_once() => _keysStorage.Received(1).Save(_storedKeys[0]);
    [Fact] void should_save_the_second_key_once() => _keysStorage.Received(1).Save(_storedKeys[1]);
}
