// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_ObserverIdentityClaims;

public abstract class when_a_generation_claims_first<THarness> : given.a_claims_storage<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    ObserverIdentityClaim _generationResult;
    ObserverIdentityClaim _registrationResult;
    ObserverIdentityClaim _repeated;
    async Task Establish() => _generationResult = await _claims.Claim(_store, _generation);
    async Task Because()
    {
        _registrationResult = await _claims.Claim(_store, _registration);
        _repeated = await _claims.Claim(_store, _generation);
    }
    [Fact] public void should_own_the_claim_for_the_generation() => _generationResult.ShouldEqual(_generation);
    [Fact] public void should_return_the_generation_claim_to_the_registration() => _registrationResult.ShouldEqual(_generation);
    [Fact] public void should_treat_the_same_claim_as_idempotent() => _repeated.ShouldEqual(_generation);
    [Fact] public async Task should_keep_the_generation_claim() => (await _claims.Get(_store, _observerId)).ShouldEqual(_generation);
}
