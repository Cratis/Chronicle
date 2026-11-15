// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.Signing.for_WebhookSigningKeyRingStorageProvider;

public class when_reading_state : given.a_storage_provider
{
    Task Because() => _provider.ReadStateAsync("state", _grainId, _grainState);

    [Fact] void should_hold_the_stored_keys_of_the_event_store() => _grainState.State.Keys.ShouldContainOnly(_storedKeys);
}
