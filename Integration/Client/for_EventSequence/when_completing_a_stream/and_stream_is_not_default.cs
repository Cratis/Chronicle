// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_completing_a_stream.and_stream_is_not_default.context;
using IAppendResult = Cratis.Chronicle.EventSequences.IAppendResult;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_completing_a_stream;

[Collection(ChronicleCollection.Name)]
public class and_stream_is_not_default(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification(chronicleFixture)
    {
        public EventSourceId EventSourceId { get; } = "source";
        public EventStreamType EventStreamType { get; } = new("invoices");
        public EventStreamId EventStreamId { get; } = new("invoices");

        public bool CompletedSuccessfully { get; private set; }
        public IAppendResult AppendAfterCompletionResult { get; private set; } = null!;

        public override IEnumerable<Type> EventTypes => [typeof(SomeStreamEvent)];

        async Task Because()
        {
            await EventStore.EventLog.Append(EventSourceId, new SomeStreamEvent("first"), EventStreamType, EventStreamId);
            var completeResult = await EventStore.EventLog.CompleteStream(EventStreamType, EventStreamId);
            CompletedSuccessfully = completeResult.IsSuccess;
            AppendAfterCompletionResult = await EventStore.EventLog.Append(EventSourceId, new SomeStreamEvent("second"), EventStreamType, EventStreamId);
        }
    }

    [Fact] void should_complete_successfully() => Context.CompletedSuccessfully.ShouldBeTrue();
    [Fact] void should_reject_subsequent_append() => Context.AppendAfterCompletionResult.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_stream_completed_constraint_violation() => Context.AppendAfterCompletionResult.ConstraintViolations.Any(v => v.ConstraintType == ConstraintType.StreamClosed).ShouldBeTrue();
}
