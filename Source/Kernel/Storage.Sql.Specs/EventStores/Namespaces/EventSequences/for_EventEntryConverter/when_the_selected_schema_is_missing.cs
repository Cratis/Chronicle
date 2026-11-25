// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventEntryConverter;

public class when_the_selected_schema_is_missing : given.a_schema_aware_read
{
    void Establish()
    {
        schema = null;
        entry.Content = """{"1":{"value":"first"},"2":{"value":"second","number":42,"empty":null}}""";
    }

    async Task Because() => result = await Read();

    [Fact] void should_resolve_the_highest_generation_schema() => resolved_type.ShouldEqual(new EventType("sample", 2));
    [Fact] void should_keep_untyped_content() => ((IDictionary<string, object?>)result.Content)["value"].ShouldEqual("second");
    [Fact] void should_keep_untyped_numbers() => ((IDictionary<string, object?>)result.Content)["number"].ShouldEqual(42L);
    [Fact] void should_keep_untyped_nulls() => ((IDictionary<string, object?>)result.Content)["empty"].ShouldBeNull();
    [Fact] void should_keep_the_selected_hash() => result.Context.Hash.ShouldEqual(new EventHash("second-hash"));
}
