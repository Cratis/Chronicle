// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.Migrations.for_MigrateExistingEventsForTypeStep.when_performing;

public class and_an_erased_subject_has_a_new_key_lifecycle : given.protected_events
{
    const string Subject = "erased-owner";

    protected override string MigrationSuffix => string.Empty;

    async Task Establish()
    {
        await _keys.RecordErasureFor(Store, Namespace, Subject);
        await _keys.DeleteFor(Store, Namespace, Subject);
        await _keys.AllowNewKeyFor(Store, Namespace, Subject);
        var content = await _manager.Apply(Store, Namespace, _sourceSchema, Subject, new JsonObject { ["name"] = "new lifecycle value" });
        var storedContent = _converter.ToExpandoObject(content, _sourceSchema);
        _stored[2] = new Dictionary<EventTypeGeneration, ExpandoObject> { [1] = storedContent };
        _events[1] = new AppendedEvent(_events[1].Context with { Subject = Subject }, storedContent);
    }

    async Task Because() => await Perform();

    [Fact] void should_complete_the_migration() => _result.TryGetException(out _).ShouldBeFalse();
    [Fact] void should_replace_both_events() => _stored.Values.All(generations => generations.ContainsKey(2)).ShouldBeTrue();
    [Fact] void should_migrate_the_old_value_as_erased() => _migrationInputs[0].ShouldEqual(string.Empty);
    [Fact] async Task should_release_the_old_target_as_erased() => (await _manager.ReleaseStrict(Store, Namespace, _targetSchema, Subject, Target(1)))["renamed"]!.GetValue<string>().ShouldEqual(string.Empty);
    [Fact] void should_keep_the_new_target_protected() => Target(2)["renamed"]!.GetValue<string>().ShouldNotEqual("new lifecycle value");
    [Fact] async Task should_release_the_new_target_under_the_new_key() => (await _manager.ReleaseStrict(Store, Namespace, _targetSchema, Subject, Target(2)))["renamed"]!.GetValue<string>().ShouldEqual("new lifecycle value");
    [Fact] async Task should_keep_the_old_source_erased() => (await _manager.ReleaseStrict(Store, Namespace, _sourceSchema, Subject, _converter.ToJsonObject(_stored[1][1], _sourceSchema)))["name"]!.GetValue<string>().ShouldEqual(string.Empty);
}
