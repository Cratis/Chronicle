// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Events.Constraints.V2;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorageV2;

public class when_clearing_one_scope : given.a_v2_store
{
    readonly List<bool> _isolated = [];

    async Task Because()
    {
        foreach (var definition in new[] { _definition, _perValue })
        {
            _storage = new();
            _definition = definition;
            await Seed();
            (await _storage.Save(_source, definition, 11, _value, _otherScope, _authority)).IsSuccess.ShouldBeTrue();
            (await _storage.ClearValues(definition, _scope, _authority)).IsSuccess.ShouldBeTrue();
            _isolated.Add(!(await _storage.GetFor(_source, definition, _scope)).Any() && (await _storage.GetFor(_source, definition, _otherScope)).SequenceEqual([_value]));
        }
    }

    [Fact] void should_clear_only_the_selected_scope_in_both_modes() => _isolated.ShouldEqual([true, true]);
    [Fact] async Task should_preserve_authority() => (await _storage.GetAuthority(_definition.Name)).ShouldEqual((Option<UniqueConstraintWriteAuthority>)_authority);
}
