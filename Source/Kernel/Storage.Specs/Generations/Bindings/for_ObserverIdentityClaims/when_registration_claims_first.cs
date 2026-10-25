// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_ObserverIdentityClaims;

public abstract class when_registration_claims_first<THarness> : given.a_claims_storage<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    ObserverIdentityClaim _registrationResult;
    ObserverIdentityClaim _generationResult;
    async Task Establish() => _registrationResult = await _claims.Claim(_store, _registration);
    async Task Because() => _generationResult = await _claims.Claim(_store, _generation);
    [Fact] public void should_own_the_claim_for_the_registration() => _registrationResult.ShouldEqual(_registration);
    [Fact] public void should_return_the_registration_claim_to_the_generation() => _generationResult.ShouldEqual(_registration);
    [Fact] public async Task should_keep_the_registration_claim() => (await _claims.Get(_store, _observerId)).ShouldEqual(_registration);
}
