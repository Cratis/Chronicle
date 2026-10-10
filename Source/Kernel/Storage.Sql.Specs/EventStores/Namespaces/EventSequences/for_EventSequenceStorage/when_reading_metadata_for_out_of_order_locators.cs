// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage;

public class when_reading_metadata_for_out_of_order_locators : given.an_event_sequence_storage
{
    IReadOnlyList<StoredEventMetadata> _result;

    void Establish()
    {
        using var context = CreateContext();
        foreach (var number in new[] { 43UL, 42UL })
        {
            context.Events.Add(new EventEntry
            {
                SequenceNumber = number,
                Type = _eventType.Id,
                CorrelationId = CorrelationId.New().Value.ToString(),
                CausedBy = "[]",
                Causation = "[]",
                Content = "not valid JSON",
                Tags = "[]"
            });
        }
        context.SaveChanges();
    }

    async Task Because() => _result = await _storage.GetMetadataAt([43UL, 1000UL, 42UL, 43UL]);

    [Fact] void should_return_metadata_in_sequence_order() => _result.Select(_ => _.SequenceNumber.Value).SequenceEqual([42UL, 43UL]).ShouldBeTrue();
    [Fact] void should_omit_duplicates_and_missing_locators() => _result.Count.ShouldEqual(2);
}
