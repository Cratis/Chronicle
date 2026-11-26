// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints.V2;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorageV2;

public class when_value_tokens_are_invalid : given.a_seeded_v2_store
{
    readonly List<Exception> _errors = [];

    async Task Because()
    {
        foreach (var text in new[] { "", "plaintext", new string('a', 63), new string('a', 65), new string('A', 64), new string('g', 64), new string(' ', 64) })
        {
            var value = new UniqueConstraintValue(text);
            _errors.Add(await Catch.Exception(async () => await _storage.Save(_source, _definition, 20, value, _scope, _authority)));
            _errors.Add(await Catch.Exception(async () => await _storage.RemoveValue(_source, _perValue, value, _scope, _authority)));
            _errors.Add(await Catch.Exception(async () => await _storage.IsAllowed(_source, _definition, value, _scope)));
        }
    }

    [Fact] void should_reject_every_invalid_token_before_mutation() => _errors.TrueForAll(_ => _ is InvalidUniqueConstraintV2Input).ShouldBeTrue();
    [Fact] void should_not_include_token_contents_in_diagnostics() => _errors.Select(_ => _.Message).Distinct().Count().ShouldEqual(1);
    [Fact] async Task should_keep_the_original_claim() => (await _storage.GetFor(_source, _definition, _scope)).ShouldEqual([_value]);
}
