// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_SinkParity.when_reducing_string_arrays;

[Collection(MongoDBCollection.Name)]
public class and_the_subject_is_erased(MongoDBFixture fixture) : given.a_string_array_round_trip(fixture)
{
    string[] _inMemoryUsers;
    string[] _mongoUsers;
    string[] _inMemoryAssignees;
    string[] _mongoAssignees;
    bool _hasKey;
    string[] _writtenUsers;

    protected override string ArrayMetadata => "\"compliance\": [{\"metadataType\":\"PII\",\"details\":\"\"}],";

    async Task Because()
    {
        await ApplyStates();
        await _keys.RecordErasureFor("test-store", "test-namespace", "root-1");
        await _keys.DeleteFor("test-store", "test-namespace", "root-1");
        var memory = await _compliance.Release("test-store", "test-namespace", CreateSchema(), InMemoryResult!);
        var mongo = await _compliance.Release("test-store", "test-namespace", CreateSchema(), MongoResult!);
        _inMemoryUsers = ReadArray(memory, "involvedUsers");
        _mongoUsers = ReadArray(mongo, "involvedUsers");
        _inMemoryAssignees = ReadArray(memory, "assigneeLogins");
        _mongoAssignees = ReadArray(mongo, "assigneeLogins");
        var written = await _compliance.Apply("test-store", "test-namespace", CreateSchema(), "root-1", Expando(("involvedUsers", new[] { "new-value" })));
        _hasKey = await _keys.HasFor("test-store", "test-namespace", "root-1");
        _writtenUsers = ReadArray(await _compliance.Release("test-store", "test-namespace", CreateSchema(), written), "involvedUsers");
    }

    [Fact] void should_release_erased_in_memory_users_as_a_typed_empty_array() => _inMemoryUsers.ShouldBeEmpty();
    [Fact] void should_release_erased_mongodb_users_as_a_typed_empty_array() => _mongoUsers.ShouldBeEmpty();
    [Fact] void should_release_erased_in_memory_assignees_as_a_typed_empty_array() => _inMemoryAssignees.ShouldBeEmpty();
    [Fact] void should_release_erased_mongodb_assignees_as_a_typed_empty_array() => _mongoAssignees.ShouldBeEmpty();
    [Fact] void should_not_resurrect_the_erased_subject_on_write() => _hasKey.ShouldBeFalse();
    [Fact] void should_not_retain_new_personal_values_for_the_erased_subject() => _writtenUsers.ShouldBeEmpty();

    string[] ReadArray(ExpandoObject state, string property)
    {
        var json = new Cratis.Chronicle.Json.ExpandoObjectConverter(new TypeFormats()).ToJsonObject(state, CreateSchema());
        return JsonSerializer.Deserialize<string[]>(json[property]!.ToJsonString())!;
    }
}
