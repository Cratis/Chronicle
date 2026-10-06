// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Migrations.for_MigrateExistingEventsForTypeStep.when_performing;

public class and_events_are_protected : given.protected_events
{
    async Task Because() => await Perform();

    [Fact] void should_complete_without_an_exception() => _result.TryGetException(out _).ShouldBeFalse();
    [Fact] void should_replace_both_events() => _stored.Values.All(generations => generations.ContainsKey(2)).ShouldBeTrue();
    [Fact] void should_migrate_plaintext() => _migrationInputs.ShouldContainOnly("Jane", "Jane");
    [Fact] void should_store_protected_target_content() => Target(1)["renamed"]!.GetValue<string>().ShouldNotEqual("Jane migrated");
    [Fact] async Task should_use_the_original_subject() => (await _manager.Release(Store, Namespace, _targetSchema, "erased-owner", Target(1)))["renamed"]!.GetValue<string>().ShouldEqual("Jane migrated");
}
