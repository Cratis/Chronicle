// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Identities;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventEntryConverter.when_converting_stored_generations;

public class and_content_and_hashes_are_preserved : Specification
{
    AppendedEvent _single;
    AppendedEvent _multiple;
    AppendedEvent _revised;
    AppendedEvent _missingHashes;
    JsonSchema _schema;

    void Establish() => _schema = JsonSchema.FromJson("""{"type":"object","properties":{"value":{"type":"string"},"flags":{"type":"array","items":{"type":"boolean"}}}}""");

    async Task Because()
    {
        _single = await Convert(new EventEntry { Content = """{"1":{"value":"first","flags":[true,false]}}""", ContentHashes = """{"1":"first-hash"}""" });
        _multiple = await Convert(new EventEntry { Content = """{"1":{"value":"first"},"2":{"value":"second"}}""", ContentHashes = """{"1":"first-hash","2":"second-hash"}""" });
        _revised = await Convert(new EventEntry
        {
            Content = """{"1":{"value":"revised"},"2":{"value":"second"}}""",
            ContentHashes = """{"1":"revised-hash","2":"second-hash"}""",
            RevisedGeneration = 1,
            OriginalContent = """{"format":1,"content":{"1":{"value":"original"}},"hashes":{"1":"original-hash"}}"""
        });
        _missingHashes = await Convert(new EventEntry { Content = """{"1":{"value":"first"}}""", ContentHashes = "null" });
    }

    [Fact] void should_read_single_generation_content() => ((IDictionary<string, object?>)_single.Content)["value"].ShouldEqual("first");
    [Fact] void should_preserve_boolean_values() => ((object[])((IDictionary<string, object?>)_single.Content)["flags"]!).ShouldEqual(new object[] { true, false });
    [Fact] void should_preserve_single_generation_json() => _single.GenerationalContent[1].ShouldEqual("""{"value":"first","flags":[true,false]}""");
    [Fact] void should_preserve_the_single_generation_hash() => _single.Context.Hash.ShouldEqual(new EventHash("first-hash"));
    [Fact] void should_read_the_highest_generation() => _multiple.Context.EventType.Generation.ShouldEqual(new EventTypeGeneration(2));
    [Fact] void should_read_highest_generation_content() => ((IDictionary<string, object?>)_multiple.Content)["value"].ShouldEqual("second");
    [Fact] void should_keep_each_generation_json() => _multiple.GenerationalContent.ShouldEqual(new Dictionary<int, string> { [1] = """{"value":"first"}""", [2] = """{"value":"second"}""" });
    [Fact] void should_keep_each_generation_hash() => _multiple.GenerationalHashes.ShouldEqual(new Dictionary<int, EventHash> { [1] = "first-hash", [2] = "second-hash" });
    [Fact] void should_read_the_highest_generation_hash() => _multiple.Context.Hash.ShouldEqual(new EventHash("second-hash"));
    [Fact] void should_read_the_revision_instead_of_the_original() => ((IDictionary<string, object?>)_revised.Content)["value"].ShouldEqual("revised");
    [Fact] void should_read_the_revision_hash() => _revised.Context.Hash.ShouldEqual(new EventHash("revised-hash"));
    [Fact] void should_keep_the_original_generation_json() => _revised.GenerationalContent[1].ShouldEqual("""{"value":"original"}""");
    [Fact] void should_keep_the_original_generation_hash() => _revised.GenerationalHashes[1].ShouldEqual(new EventHash("original-hash"));
    [Fact] void should_use_an_unset_hash_when_missing() => _missingHashes.Context.Hash.ShouldEqual(EventHash.NotSet);
    [Fact] void should_have_no_generational_hashes_when_missing() => _missingHashes.GenerationalHashes.ShouldBeEmpty();

    Task<AppendedEvent> Convert(EventEntry entry)
    {
        entry.Type = "type";
        entry.CorrelationId = Guid.Empty.ToString();
        return EventEntryConverter.ToAppendedEvent(
            entry,
            "store",
            "namespace",
            Substitute.For<IIdentityStorage>(),
            [],
            _ => Task.FromResult<JsonSchema?>(_schema),
            new ExpandoObjectConverter(new TypeFormats()));
    }
}
