// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage;

public class when_appending_decimal_content : given.an_event_sequence_storage
{
    AppendedEvent _read;
    AppendedEvent _cursorRead;

    void Establish()
    {
        _eventTypesStorage.HasFor(_eventType.Id, _eventType.Generation).Returns(true);
        _eventTypesStorage.GetFor(_eventType.Id, _eventType.Generation).Returns(new EventTypeSchema(
            _eventType,
            EventTypeOwner.Client,
            EventTypeSource.Code,
            JsonSchema.FromJson("""{"type":"object","properties":{"amount":{"type":"number","format":"decimal"},"precise":{"type":"number","format":"decimal"}}}""")));
    }

    async Task Because()
    {
        dynamic content = new ExpandoObject();
        content.amount = 193.58m;
        content.precise = 1234567890.123456789012345678m;
        (await Append(EventSequenceNumber.First, (ExpandoObject)content, EventHash.NotSet)).IsSuccess.ShouldBeTrue();
        _read = await _storage.GetEventAt(EventSequenceNumber.First);
        using var cursor = await _storage.GetFromSequenceNumber(EventSequenceNumber.First);
        (await cursor.MoveNext()).ShouldBeTrue();
        _cursorRead = cursor.Current.Single();
    }

    [Fact] void should_preserve_amount_bits() => decimal.GetBits((decimal)((IDictionary<string, object?>)_read.Content)["amount"]!).ShouldEqual(decimal.GetBits(193.58m));
    [Fact] void should_preserve_all_significant_digits() => decimal.GetBits((decimal)((IDictionary<string, object?>)_read.Content)["precise"]!).ShouldEqual(decimal.GetBits(1234567890.123456789012345678m));
    [Fact] void should_use_the_schema_on_cursor_reads() => decimal.GetBits((decimal)((IDictionary<string, object?>)_cursorRead.Content)["precise"]!).ShouldEqual(decimal.GetBits(1234567890.123456789012345678m));
}
