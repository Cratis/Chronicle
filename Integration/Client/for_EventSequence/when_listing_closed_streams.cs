// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_listing_closed_streams.context;

namespace Cratis.Chronicle.Integration.for_EventSequence;

[Collection(ChronicleCollection.Name)]
public class when_listing_closed_streams(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.an_event_log_with_closing_streams(fixture)
    {
        public ClosedStream[] All;
        public ClosedStream[] Filtered;

        async Task Establish()
        {
            await Append(new given.StreamClosed());
            await EventStore.EventLog.CompleteStream(ClosedStreamScope.ForEventSource("other"));
        }

        async Task Because()
        {
            All = [.. await EventStore.EventLog.GetClosedStreams()];
            Filtered = [.. await EventStore.EventLog.GetClosedStreams(ClosedStreamScope.ForEventSource(Source))];
        }
    }

    [Fact] void should_list_both_origins() => Context.All.Select(closure => closure.Origin).ShouldContainOnly(ClosedStreamOrigin.ClosingEvent, ClosedStreamOrigin.CompleteStream);
    [Fact] void should_filter_to_the_source() => Context.Filtered.Single().Scope.ShouldEqual(Context.Scope);
    [Fact] void should_name_the_closing_constraint() => Context.Filtered.Single().ClosedBy!.Value.ShouldEqual(nameof(given.ClosingStream));
    [Fact] void should_preserve_the_closing_sequence_number() => Context.Filtered.Single().SequenceNumber.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_preserve_the_closing_time() => Context.Filtered.Single().ClosedAt.ShouldNotBeNull();
}
