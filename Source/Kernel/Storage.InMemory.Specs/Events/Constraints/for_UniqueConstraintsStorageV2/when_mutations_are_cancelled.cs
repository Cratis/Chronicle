// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Events.Constraints.V2;
using Cratis.Monads;
using Catch = Cratis.Specifications.Catch;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorageV2;

public class when_mutations_are_cancelled : given.a_seeded_v2_store
{
    readonly List<Exception> _errors = [];

    async Task Because()
    {
        var token = new CancellationToken(true);
        for (var operation = 0; operation < 6; operation++)
        {
            _errors.Add(await Catch.Exception(async () => await Mutation(operation, _authority, token)));
        }

        _errors.Add(await Catch.Exception(async () => await _storage.InitializeAuthority("missing", _authority, token)));
        _errors.Add(await Catch.Exception(async () => await _storage.TryTransition(_definition.Name, _authority, Run(1), token)));
    }

    [Fact] void should_cancel_every_mutation_and_authority_write() => _errors.TrueForAll(_ => _ is OperationCanceledException).ShouldBeTrue();
    [Fact] async Task should_preserve_claims() => (await _storage.GetFor(_source, _definition, _scope)).ShouldEqual([_value]);
    [Fact] async Task should_preserve_authority() => (await _storage.GetAuthority(_definition.Name)).ShouldEqual((Option<UniqueConstraintWriteAuthority>)_authority);
    [Fact] async Task should_not_initialize_authority() => (await _storage.GetAuthority("missing")).HasValue.ShouldBeFalse();
}
