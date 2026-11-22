// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Events.Constraints.V2;
using Cratis.Monads;

using Catch = Cratis.Specifications.Catch;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorageV2;

public class when_authority_inputs_are_null : given.a_seeded_v2_store
{
    readonly List<Exception> _errors = [];

    async Task Because()
    {
        foreach (var proof in new UniqueConstraintWriteAuthority[] { null!, _authority with { Phase = null! } })
        {
            for (var operation = 0; operation < 6; operation++)
            {
                _errors.Add(await Catch.Exception(async () => await Mutation(operation, proof)));
            }

            _errors.Add(await Catch.Exception(async () => await _storage.InitializeAuthority("missing", proof)));
            _errors.Add(await Catch.Exception(async () => await _storage.TryTransition(_definition.Name, proof, Run(1))));
        }

        _errors.Add(await Catch.Exception(async () => await _storage.TryTransition(_definition.Name, _authority, null!)));
    }

    [Fact] void should_check_every_mutation_and_authority_write() => _errors.Count.ShouldEqual(17);
    [Fact] void should_reject_every_null_input_explicitly() => _errors.TrueForAll(_ => _ is InvalidUniqueConstraintV2Input).ShouldBeTrue();
    [Fact] async Task should_preserve_claims() => (await _storage.GetFor(_source, _definition, _scope)).ShouldEqual([_value]);
    [Fact] async Task should_preserve_authority() => (await _storage.GetAuthority(_definition.Name)).ShouldEqual((Option<UniqueConstraintWriteAuthority>)_authority);
    [Fact] async Task should_not_initialize_invalid_authority() => (await _storage.GetAuthority("missing")).HasValue.ShouldBeFalse();
}
