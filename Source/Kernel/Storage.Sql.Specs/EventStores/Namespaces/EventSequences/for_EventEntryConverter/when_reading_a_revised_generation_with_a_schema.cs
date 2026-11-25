// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventEntryConverter;

public class when_reading_a_revised_generation_with_a_schema : given.a_schema_aware_read
{
    void Establish()
    {
        entry.RevisedGeneration = 1;
        entry.Content = """{"1":{"value":"revised"},"2":{"value":"second"}}""";
        entry.ContentHashes = """{"1":"revised-hash","2":"second-hash"}""";
        entry.OriginalContent = """{"format":1,"content":{"1":{"value":"original"}},"hashes":{"1":"original-hash"}}""";
    }

    async Task Because() => result = await Read();

    [Fact] void should_resolve_the_revised_generation_schema() => resolved_type.ShouldEqual(new EventType("sample", 1));
    [Fact] void should_resolve_the_schema_once() => schema_resolutions.ShouldEqual(1);
    [Fact] void should_read_revised_content() => ((IDictionary<string, object?>)result.Content)["value"].ShouldEqual("revised");
    [Fact] void should_keep_the_revised_context_hash() => result.Context.Hash.ShouldEqual(new EventHash("revised-hash"));
    [Fact] void should_keep_original_generation_content() => result.GenerationalContent[1].ShouldEqual("""{"value":"original"}""");
    [Fact] void should_keep_original_generation_hashes() => result.GenerationalHashes[1].ShouldEqual(new EventHash("original-hash"));
    [Fact] void should_keep_named_tags() => result.Context.NamedTags.ShouldEqual(named_tags);
    [Fact] void should_keep_the_correlation_identifier() => result.Context.CorrelationId.Value.ShouldEqual(Guid.Empty);
}
