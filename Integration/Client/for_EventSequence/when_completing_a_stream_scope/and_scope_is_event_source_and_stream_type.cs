// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_completing_a_stream_scope.and_scope_is_event_source_and_stream_type.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_completing_a_stream_scope;

[Collection(ChronicleCollection.Name)]
public class and_scope_is_event_source_and_stream_type(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.an_event_log_with_closing_streams(fixture)
    {
        public bool Completed;
        public IAppendResult Covered;
        public IAppendResult OtherSource;
        public IAppendResult OtherStream;

        async Task Because()
        {
            Completed = (await EventStore.EventLog.CompleteStream(new ClosedStreamScope(Source, EventStreamType: StreamType))).IsSuccess;
            Covered = await Append(new given.StreamActivity("covered"));
            OtherSource = await EventStore.EventLog.Append(Guid.NewGuid().ToString(), new given.StreamActivity("other"), eventStreamType: StreamType, eventStreamId: StreamId);
            OtherStream = await EventStore.EventLog.Append(Source, new given.StreamActivity("other stream"), eventStreamType: "other", eventStreamId: StreamId);
        }
    }

    [Fact] void should_complete_the_scope() => Context.Completed.ShouldBeTrue();
    [Fact] void should_reject_covered_appends() => Context.Covered.HasConstraintViolations.ShouldBeTrue();
    [Fact] void should_allow_another_source() => Context.OtherSource.IsSuccess.ShouldBeTrue();
    [Fact] void should_allow_another_stream_type() => Context.OtherStream.IsSuccess.ShouldBeTrue();
}
