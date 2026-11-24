// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventEntryConverter;

public class when_the_selected_generation_is_missing : given.a_schema_aware_read
{
    void Establish()
    {
        entry.Content = """{"01":{"value":"first"},"02":{"value":"second"}}""";
        entry.ContentHashes = """{"1":"first-hash"}""";
    }

    async Task Because() => result = await Read();

    [Fact] void should_resolve_the_selected_generation_schema() => resolved_type.ShouldEqual(new EventType("sample", 2));
    [Fact] void should_return_empty_content() => result.Content.ShouldBeEmpty();
    [Fact] void should_use_the_unset_hash() => result.Context.Hash.ShouldEqual(EventHash.NotSet);
    [Fact] void should_keep_available_generation_content() => result.GenerationalContent[2].ShouldEqual("""{"value":"second"}""");
    [Fact] void should_keep_available_generation_hashes() => result.GenerationalHashes[1].ShouldEqual(new EventHash("first-hash"));
}
