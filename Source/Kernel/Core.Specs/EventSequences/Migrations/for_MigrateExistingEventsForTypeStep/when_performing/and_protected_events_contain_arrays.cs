// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Migrations.for_MigrateExistingEventsForTypeStep.when_performing;

public class and_protected_events_contain_arrays : given.protected_events
{
    protected override bool UsesObjectArray => true;
    protected override string SourceSchemaJson => """
        {"type":"object","properties":{
          "contacts":{"type":"array","items":{"type":"object","properties":{"name":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}}}},
          "emails":{"type":"array","items":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}},
          "tags":{"type":"array","items":{"type":"string"}}
        }}
        """;
    protected override string SourceContentJson => """{"contacts":[{"name":"Jane"}],"emails":["jane@example.com"],"tags":["plain"]}""";

    async Task Because() => await Perform();

    [Fact] void should_complete_the_migration() => _result.TryGetException(out _).ShouldBeFalse();
    [Fact] void should_replace_both_events() => _stored.Values.All(generations => generations.ContainsKey(2)).ShouldBeTrue();
    [Fact] void should_migrate_the_released_member() => _migrationInputs.ShouldContainOnly("Jane", "Jane");
    [Fact] async Task should_protect_the_target_generation() => (await _manager.ReleaseStrict(Store, Namespace, _targetSchema, "active-owner", Target(2)))["renamed"]!.GetValue<string>().ShouldEqual("Jane migrated");
}
