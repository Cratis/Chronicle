// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorageV2;

public class when_retaining_values_per_value : given.a_v2_store
{
    UniqueConstraintValue[] _before;
    UniqueConstraintValue[] _after;

    async Task Establish()
    {
        _definition = _perValue;
        await Seed();
    }

    async Task Because()
    {
        (await _storage.Save(_source, _definition, 20, _otherValue, _scope, _authority)).IsSuccess.ShouldBeTrue();
        _before = (await _storage.GetFor(_source, _definition, _scope)).ToArray();
        (await _storage.RemoveValue(_source, _definition, _value, _scope, _authority)).IsSuccess.ShouldBeTrue();
        _after = (await _storage.GetFor(_source, _definition, _scope)).ToArray();
    }

    [Fact] void should_retain_both_values_before_removal() => _before.OrderBy(_ => _.Value).ShouldEqual(new[] { _value, _otherValue }.OrderBy(_ => _.Value));
    [Fact] void should_remove_only_the_selected_value() => _after.ShouldEqual([_otherValue]);
    [Fact] async Task should_release_the_selected_value() => (await _storage.IsAllowed(_otherSource, _definition, _value, _scope)).IsAllowed.ShouldBeTrue();
}
