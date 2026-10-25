// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_ObserverIdentityClaims;

public abstract class when_claims_race<THarness> : given.a_claims_storage<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    ObserverIdentityClaim[] _results;
    ObserverIdentityClaim _stored;
    async Task Because()
    {
        _results = await Task.WhenAll(Enumerable.Range(0, 10).Select(index => _claims.Claim(_store, index % 2 == 0 ? _registration : _generation)));
        _stored = await _claims.Get(_store, _observerId);
    }
    [Fact] public void should_store_exactly_one_owner() => (_stored == _registration || _stored == _generation).ShouldBeTrue();
    [Fact] public void should_let_every_caller_see_the_same_owner() => _results.All(_ => _ == _stored).ShouldBeTrue();
}
