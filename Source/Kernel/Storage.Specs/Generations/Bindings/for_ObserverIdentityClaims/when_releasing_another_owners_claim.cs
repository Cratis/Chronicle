// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_ObserverIdentityClaims;

public abstract class when_releasing_another_owners_claim<THarness> : given.a_claims_storage<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    bool _released;
    async Task Establish() => await _claims.Claim(_store, _registration);
    async Task Because() => _released = await _claims.Release(_store, _generation);
    [Fact] public void should_not_release() => _released.ShouldBeFalse();
    [Fact] public async Task should_keep_the_claim() => (await _claims.Get(_store, _observerId)).ShouldEqual(_registration);
}
