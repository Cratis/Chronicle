// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.InMemory.EventSequences;
using Cratis.Chronicle.Storage.InMemory.Identities;

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_content_round_trips_through_in_memory_storage : given.content_with_storage_conversions
{
    async Task Establish()
    {
        var sequence = new EventSequenceStorage("store", "tenant", "log", new IdentityStorage());
        var appended = await sequence.Append(EventSequenceNumber.First, EventSourceType.Default, "source", EventStreamType.All, EventStreamId.Default, new("event", 1), CorrelationId.New(), [], [], [], DateTimeOffset.UtcNow, _generations, new Dictionary<EventTypeGeneration, EventHash>());
        appended.IsSuccess.ShouldBeTrue();
        using var cursor = await sequence.GetRange(EventSequenceNumber.First, EventSequenceNumber.First);
        (await cursor.MoveNext()).ShouldBeTrue();
        _stored = cursor.Current.Single();
    }

    async Task Because() => _result = await Verify();

    [Fact] void should_recognize_the_duplicate_after_storage_conversion() => _result.Result.ShouldEqual(ContentVerificationResult.Equal);
}
