// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ReindexConstraintsStep;

public class when_reindexing_per_value_claims : given.a_unique_constraint_to_reindex
{
    EventSourceId _owner;
    EventSourceId _otherOwner;

    async Task Establish()
    {
        _storage = new Storage.InMemory.Events.Constraints.UniqueConstraintsStorage();
        _definition = _definition with { Mode = UniqueConstraintMode.PerValue };
        _validator = new(_definition, _storage);
        _owner = EventSourceId.New();
        _otherOwner = EventSourceId.New();
        await _storage.Save(_owner, _definition, 42L, "stale");
    }

    async Task Because()
    {
        await ReindexConstraintsStep.ReindexEvent(_definition, EventFor(_owner), ContentWith("SomeValue"), _seen, _validator, _storage);
        await ReindexConstraintsStep.ReindexEvent(_definition, EventFor(_owner), ContentWith("AnotherValue"), _seen, _validator, _storage);
    }

    [Fact] async Task should_keep_the_earlier_value_reserved() => (await _storage.IsAllowed(_otherOwner, _definition, "4f7aa54b8d9a8f5e7b06bf38217a84dfd7272bd50f5aebe97ae321f24eceb291")).IsAllowed.ShouldBeFalse();
    [Fact] async Task should_keep_the_later_value_reserved() => (await _storage.IsAllowed(_otherOwner, _definition, "9adcff3cccfebacaeb8f83c36b44156c772d096a4f4c41ccfa57349bdef8dd0a")).IsAllowed.ShouldBeFalse();
    [Fact] async Task should_release_stale_values_from_the_previous_index() => (await _storage.IsAllowed(_otherOwner, _definition, "stale")).IsAllowed.ShouldBeTrue();
    [Fact] void should_clear_the_source_only_once() => _seen.Count.ShouldEqual(1);
}
