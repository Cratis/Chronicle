// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Identities;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_content_round_trips_through_sql_storage : given.content_with_storage_conversions
{
    async Task Establish()
    {
        _storage.GetEventStore("store").GetNamespace("tenant").GetEventSequence("log")
            .SerializeContentForVerification(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>())
            .Returns(call => EventEntryConverter.SerializeContent(call.Arg<ExpandoObject>()));
        var entry = EventEntryConverter.ToEventEntry(EventSequenceNumber.First, EventSourceType.Default, "source", EventStreamType.All, EventStreamId.Default, new("event", 1), CorrelationId.New(), [], [], [], DateTimeOffset.UtcNow, _generations);
        _stored = await EventEntryConverter.ToAppendedEvent(entry, "store", "tenant", Substitute.For<IIdentityStorage>());
    }

    async Task Because() => _result = await Verify();

    [Fact] void should_recognize_the_duplicate_after_storage_conversion() => _result.Result.ShouldEqual(ContentVerificationResult.Equal);
}
