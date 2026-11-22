// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorageV2;

public class when_reads_are_cancelled : given.a_seeded_v2_store
{
    Exception[] _errors;

    async Task Because()
    {
        var token = new CancellationToken(true);
        _errors =
        [
            await Catch.Exception(async () => await _storage.GetAuthority(_definition.Name, token)),
            await Catch.Exception(async () => await _storage.IsAllowed(_source, _definition, _value, _scope, token)),
            await Catch.Exception(async () => await _storage.GetFor(_source, _definition, _scope, token)),
            await Catch.Exception(async () => await _storage.GetForStream(_source, _definition, _scope, token))
        ];
    }

    [Fact] void should_cancel_all_four_reads() => _errors.All(_ => _ is OperationCanceledException).ShouldBeTrue();
    [Fact] async Task should_leave_claims_unchanged() => (await _storage.GetFor(_source, _definition, _scope)).ShouldEqual([_value]);
}
