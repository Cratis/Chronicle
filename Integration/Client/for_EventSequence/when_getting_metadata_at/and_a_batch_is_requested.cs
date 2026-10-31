// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

using context = Cratis.Chronicle.Integration.for_EventSequence.when_getting_metadata_at.and_a_batch_is_requested.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_getting_metadata_at;

/// <summary>
/// Verifies a metadata batch omits missing events and preserves stream routing.
/// </summary>
/// <param name="context">The integration context.</param>
[Collection(ChronicleCollection.Name)]
public class and_a_batch_is_requested(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification<ChronicleFixture>(fixture)
    {
        public IImmutableDictionary<EventSequenceNumber, EventMetadata> Metadata;

        public override IEnumerable<Type> EventTypes => [typeof(MetadataRecorded)];

        async Task Because()
        {
            var source = Guid.NewGuid().ToString();
            var first = await EventStore.EventLog.Append(source, new MetadataRecorded("first"), "Stream", "one", "Source");
            var second = await EventStore.EventLog.Append(source, new MetadataRecorded("second"), "Stream", "two", "Source");
            Metadata = await EventStore.EventLog.GetMetadataAt([first.SequenceNumber, second.SequenceNumber, first.SequenceNumber, EventSequenceNumber.Unavailable]);
        }
    }

    [Fact] void should_return_the_two_existing_locators() => Context.Metadata.Count.ShouldEqual(2);
    [Fact] void should_preserve_stream_routing() => Context.Metadata.Values.Select(_ => _.EventStreamId.Value).ShouldContainOnly("one", "two");
    [Fact] void should_preserve_source_routing() => Context.Metadata.Values.All(_ => _.EventSourceType.Value == "Source").ShouldBeTrue();
}
