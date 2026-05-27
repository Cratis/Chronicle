// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using IAppendResult = Cratis.Chronicle.EventSequences.IAppendResult;
using context = Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_completing_a_stream.and_appending_to_other_streams_after_completion.context;

namespace Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_completing_a_stream;

[Collection(ChronicleCollection.Name)]
public class and_appending_to_other_streams_after_completion(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
    {
        public EventSourceId EventSourceId { get; } = "source";
        public EventStreamType CompletedStreamType { get; } = new("invoices");
        public EventStreamId CompletedStreamId { get; } = new("invoices");
        public EventStreamType OtherStreamType { get; } = new("orders");
        public EventStreamId OtherStreamId { get; } = new("orders");

        public IAppendResult AppendToOtherStreamResult { get; private set; } = null!;
        public IAppendResult AppendToDefaultStreamResult { get; private set; } = null!;

        public override IEnumerable<Type> EventTypes => [typeof(SomeStreamEvent)];

        async Task Because()
        {
            await EventStore.EventLog.CompleteStream(CompletedStreamType, CompletedStreamId);
            AppendToOtherStreamResult = await EventStore.EventLog.Append(EventSourceId, new SomeStreamEvent("other"), OtherStreamType, OtherStreamId);
            AppendToDefaultStreamResult = await EventStore.EventLog.Append(EventSourceId, new SomeStreamEvent("default"));
        }
    }

    [Fact] void should_succeed_appending_to_other_stream() => Context.AppendToOtherStreamResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_succeed_appending_to_default_stream() => Context.AppendToDefaultStreamResult.IsSuccess.ShouldBeTrue();
}
