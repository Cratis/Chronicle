// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventEntryConverter;

public class when_reading_redacted_content_with_a_schema : given.a_schema_aware_read
{
    void Establish()
    {
        entry.Type = GlobalEventTypes.Redaction;
        entry.Content = """{"1":{"value":"first"},"2":{"reason":"removed"}}""";
    }

    async Task Because() => result = await Read();

    [Fact] void should_not_resolve_a_schema() => schema_resolutions.ShouldEqual(0);
    [Fact] void should_return_a_redaction_event() => result.Context.EventType.Id.ShouldEqual(GlobalEventTypes.Redaction);
    [Fact] void should_return_the_redaction_reason() => ((IDictionary<string, object?>)result.Content)["reason"].ShouldEqual("removed");
    [Fact] void should_keep_the_original_generation_content() => result.GenerationalContent[2].ShouldEqual("""{"reason":"removed"}""");
    [Fact] void should_keep_the_original_generation_hashes() => result.GenerationalHashes[2].ShouldEqual(new EventHash("second-hash"));
}
