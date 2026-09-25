// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.Identities;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventEntryConverter.when_reading_an_event_with_multiple_generations;

public class and_appended_generation_is_stored : Specification
{
    EventEntry _entry;
    AppendedEvent _result;
    IIdentityStorage _identities;

    void Establish()
    {
        _entry = new EventEntry
        {
            Type = new EventTypeId(Guid.NewGuid().ToString()),
            CorrelationId = Guid.NewGuid().ToString(),
            Generation = 1,
            Content = "{\"1\":{\"name\":\"original\"},\"2\":{\"name\":\"migrated\"}}"
        };
        _identities = Substitute.For<IIdentityStorage>();
        _identities.GetFor(Arg.Any<IEnumerable<IdentityId>>()).Returns(Identity.NotSet);
    }

    async Task Because() => _result = await EventEntryConverter.ToAppendedEvent(_entry, EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identities);

    [Fact] void should_report_the_appended_generation() => _result.Context.EventType.Generation.ShouldEqual((EventTypeGeneration)1);
    [Fact] void should_return_the_appended_content() => ((IDictionary<string, object?>)_result.Content)["name"].ShouldEqual("original");
    [Fact] void should_keep_the_migrated_content() => _result.GenerationalContent[2].ShouldContain("migrated");
}
