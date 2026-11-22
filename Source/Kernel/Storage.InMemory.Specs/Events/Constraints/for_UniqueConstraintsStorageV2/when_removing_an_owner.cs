// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorageV2;

public class when_removing_an_owner : given.a_v2_store
{
    bool[] _isolated;

    async Task Because()
    {
        var results = new List<bool>();
        foreach (var definition in new[] { _definition, _perValue })
        {
            _storage = new();
            _definition = definition;
            await Seed();
            (await _storage.Save(_source, definition, 11, _value, _otherScope, _authority)).IsSuccess.ShouldBeTrue();
            (await _storage.Save(_otherSource, definition, 12, _otherValue, _scope, _authority)).IsSuccess.ShouldBeTrue();
            (await _storage.Remove(_source, definition, _scope, _authority)).IsSuccess.ShouldBeTrue();
            results.Add(!(await _storage.GetFor(_source, definition, _scope)).Any() && (await _storage.GetFor(_source, definition, _otherScope)).SequenceEqual([_value]) && (await _storage.GetFor(_otherSource, definition, _scope)).SequenceEqual([_otherValue]));
        }
        _isolated = results.ToArray();
    }

    [Fact] void should_remove_only_the_owner_in_the_selected_scope_in_both_modes() => _isolated.ShouldEqual([true, true]);
}
