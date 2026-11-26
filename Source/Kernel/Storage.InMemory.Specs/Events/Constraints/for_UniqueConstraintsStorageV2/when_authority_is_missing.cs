// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Events.Constraints.V2;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorageV2;

public class when_authority_is_missing : given.a_v2_store
{
    Result<bool, UniqueConstraintWriteFailure>[] _results;

    Result<UniqueConstraintWriteAuthority, UniqueConstraintWriteFailure> _transition;

    async Task Because()
    {
        _results = await Task.WhenAll(Mutations(_authority));
        _transition = await _storage.TryTransition(_definition.Name, _authority, Run(1));
    }

    [Fact] void should_refuse_the_transition() => _transition.AsT1.ShouldEqual(UniqueConstraintWriteFailure.MissingAuthority);
    [Fact] void should_refuse_all_six_mutations() => _results.All(_ => !_.IsSuccess && _.AsT1 == UniqueConstraintWriteFailure.MissingAuthority).ShouldBeTrue();
    [Fact] async Task should_not_create_authority() => (await _storage.GetAuthority(_definition.Name)).HasValue.ShouldBeFalse();
    [Fact] async Task should_not_create_claims() => (await _storage.GetFor(_source, _definition, _scope)).ShouldBeEmpty();
}
