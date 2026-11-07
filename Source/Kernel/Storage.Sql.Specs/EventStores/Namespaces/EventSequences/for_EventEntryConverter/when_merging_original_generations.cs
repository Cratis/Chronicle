// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Identities;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventEntryConverter;

public class when_merging_original_generations : Specification
{
    AppendedEvent _event;

    async Task Because()
    {
        var entry = new EventEntry
        {
            Type = "type",
            CorrelationId = Guid.NewGuid().ToString(),
            Content = "{\"1\":{\"value\":\"revised\"},\"3\":{\"value\":\"derived\"}}",
            ContentHashes = "{\"1\":\"current-hash\",\"3\":\"derived-hash\"}",
            RevisedGeneration = 1,
            OriginalContent = "{\"format\":1,\"content\":{\"1\":{\"value\":\"original\"},\"2\":{\"value\":\"second\"}},\"hashes\":{\"1\":\"original-hash\",\"2\":\"second-hash\"}}"
        };
        _event = await EventEntryConverter.ToAppendedEvent(entry, "store", "namespace", Substitute.For<IIdentityStorage>());
    }

    [Fact] void should_keep_current_content_separate() => ((IDictionary<string, object?>)_event.Content)["value"].ShouldEqual("revised");
    [Fact] void should_keep_current_context_hash() => _event.Context.Hash.Value.ShouldEqual("current-hash");
    [Fact] void should_use_original_generation_one() => _event.OriginalContent.ShouldContain("original");
    [Fact] void should_prefer_the_original_hash_on_a_collision() => _event.GenerationalHashes[1].Value.ShouldEqual("original-hash");
    [Fact] void should_prefer_the_original_content_on_a_collision() => _event.GenerationalContent[1].ShouldContain("original");
    [Fact] void should_include_original_only_generations() => _event.GenerationalContent.ContainsKey(2).ShouldBeTrue();
    [Fact] void should_include_legacy_only_generations() => _event.GenerationalHashes[3].Value.ShouldEqual("derived-hash");
}
