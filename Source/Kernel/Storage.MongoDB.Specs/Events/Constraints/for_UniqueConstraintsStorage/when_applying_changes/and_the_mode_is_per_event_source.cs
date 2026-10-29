// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.for_UniqueConstraintsStorage.when_applying_changes;

[Collection(MongoDBCollection.Name)]
public class and_the_mode_is_per_event_source(MongoDBFixture fixture) : given.a_unique_constraints_storage(fixture)
{
    IReadOnlyCollection<int> _refused;

    async Task Establish() => await _storage.Save("released", _definition, 1, "freed", "scope");

    async Task Because() => _refused = await _storage.ApplyChanges(
        _definition,
        [
            new(UniqueConstraintIndexChangeKind.Save, "first", 2, "scope", "one"),
            new(UniqueConstraintIndexChangeKind.Save, "first", 3, "scope", "two"),
            new(UniqueConstraintIndexChangeKind.Save, "second", 4, "scope", "two"),
            new(UniqueConstraintIndexChangeKind.Remove, "released", 5, "scope"),
            new(UniqueConstraintIndexChangeKind.Save, "third", 6, "scope", "freed", new Subject("a subject")),
            new(UniqueConstraintIndexChangeKind.Save, "fourth", 7, "other-scope", "two")
        ]);

    [Fact] void should_refuse_only_the_claim_on_a_value_another_event_source_holds() => _refused.ShouldContainOnly(2);
    [Fact] async Task should_move_the_claim_of_an_event_source_to_its_latest_value() => (await _storage.GetFor("first", _definition, "scope")).Select(_ => _.Value).ShouldContainOnly("two");
    [Fact] async Task should_leave_the_refused_event_source_without_a_claim() => (await _storage.GetFor("second", _definition, "scope")).ShouldBeEmpty();
    [Fact] async Task should_release_the_removed_claim() => (await _storage.GetFor("released", _definition, "scope")).ShouldBeEmpty();
    [Fact] async Task should_apply_the_changes_after_the_refused_claim() => (await _storage.IsAllowed("anyone", _definition, "freed", "scope")).ShouldEqual((false, (EventSequenceNumber)6UL));
    [Fact] async Task should_apply_changes_in_another_scope_on_their_own() => (await _storage.GetFor("fourth", _definition, "other-scope")).Select(_ => _.Value).ShouldContainOnly("two");
}
