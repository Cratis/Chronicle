// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.for_UniqueConstraintsStorage.when_applying_changes;

[Collection(MongoDBCollection.Name)]
public class and_the_mode_is_per_value(MongoDBFixture fixture) : given.a_unique_constraints_storage(fixture)
{
    IReadOnlyCollection<int> _refused;

    async Task Establish()
    {
        _definition = _definition with { Mode = UniqueConstraintMode.PerValue };
        await _storage.Save("leaving", _definition, 1, "kept-until-released", "scope");
        await _storage.Save("leaving", _definition, 2, "released", "scope");
        await _storage.Save("gone", _definition, 3, "everything", "scope");
    }

    async Task Because() => _refused = await _storage.ApplyChanges(
        _definition,
        [
            new(UniqueConstraintIndexChangeKind.Save, "first", 4, "scope", "one"),
            new(UniqueConstraintIndexChangeKind.Save, "first", 5, "scope", "two"),
            new(UniqueConstraintIndexChangeKind.Save, "first", 6, "scope", "one"),
            new(UniqueConstraintIndexChangeKind.Save, "second", 7, "scope", "one"),
            new(UniqueConstraintIndexChangeKind.RemoveValue, "leaving", 8, "scope", "released"),
            new(UniqueConstraintIndexChangeKind.Remove, "gone", 9, "scope"),
            new(UniqueConstraintIndexChangeKind.Save, "third", 10, "scope", "released")
        ]);

    [Fact] void should_refuse_only_the_claim_on_a_value_another_event_source_holds() => _refused.ShouldContainOnly(3);
    [Fact] async Task should_retain_every_value_an_event_source_claims() => (await _storage.GetFor("first", _definition, "scope")).Select(_ => _.Value).ShouldContainOnly("one", "two");
    [Fact] async Task should_keep_the_first_claim_on_a_reclaimed_value() => (await _storage.IsAllowed("anyone", _definition, "one", "scope")).ShouldEqual((false, (EventSequenceNumber)4UL));
    [Fact] async Task should_release_only_the_released_value() => (await _storage.GetFor("leaving", _definition, "scope")).Select(_ => _.Value).ShouldContainOnly("kept-until-released");
    [Fact] async Task should_release_every_value_of_a_removed_event_source() => (await _storage.GetFor("gone", _definition, "scope")).ShouldBeEmpty();
    [Fact] async Task should_let_a_released_value_be_claimed_later_in_the_same_changes() => (await _storage.GetFor("third", _definition, "scope")).Select(_ => _.Value).ShouldContainOnly("released");
}
