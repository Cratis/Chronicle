// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Migrations.for_MigrateExistingEventsForTypeStep.when_performing;

public class and_a_subject_was_erased : given.protected_events
{
    async Task Establish()
    {
        await _keys.RecordErasureFor(Store, Namespace, "erased-owner");
        await _keys.DeleteFor(Store, Namespace, "erased-owner");
    }

    async Task Because() => await Perform();

    [Fact] void should_complete_without_an_exception() => _result.TryGetException(out _).ShouldBeFalse();
    [Fact] void should_replace_both_events() => _stored.Values.All(generations => generations.ContainsKey(2)).ShouldBeTrue();
    [Fact] void should_keep_the_erased_target_empty() => Target(1)["renamed"]!.GetValue<string>().ShouldEqual(string.Empty);
    [Fact] void should_migrate_erased_plaintext() => _migrationInputs[0].ShouldEqual(string.Empty);
    [Fact] void should_migrate_active_plaintext() => _migrationInputs[1].ShouldEqual("Jane");
    [Fact] void should_protect_the_active_target() => Target(2)["renamed"]!.GetValue<string>().ShouldNotEqual("Jane migrated");
    [Fact] async Task should_release_the_active_target_under_its_original_subject() => (await _manager.Release(Store, Namespace, _targetSchema, "active-owner", Target(2)))["renamed"]!.GetValue<string>().ShouldEqual("Jane migrated");
    [Fact] async Task should_not_revive_the_erased_key() => (await _keys.TryGetFor(Store, Namespace, "erased-owner")).ShouldBeNull();
}
