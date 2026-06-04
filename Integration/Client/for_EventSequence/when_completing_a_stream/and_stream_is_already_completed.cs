// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_completing_a_stream.and_stream_is_already_completed.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_completing_a_stream;

[Collection(ChronicleCollection.Name)]
public class and_stream_is_already_completed(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification(chronicleFixture)
    {
        public EventStreamType EventStreamType { get; } = new("invoices");
        public EventStreamId EventStreamId { get; } = new("invoices");

        public bool FirstResultSucceeded { get; private set; }
        public bool SecondResultSucceeded { get; private set; } = true;
        public string SecondFailureName { get; private set; } = string.Empty;

        public override IEnumerable<Type> EventTypes => [typeof(SomeStreamEvent)];

        async Task Because()
        {
            var first = await EventStore.EventLog.CompleteStream(EventStreamType, EventStreamId);
            FirstResultSucceeded = first.IsSuccess;

            var second = await EventStore.EventLog.CompleteStream(EventStreamType, EventStreamId);
            SecondResultSucceeded = second.IsSuccess;
            if (second.TryGetError(out var error))
            {
                SecondFailureName = error.ToString();
            }
        }
    }

    [Fact] void should_complete_first_request_successfully() => Context.FirstResultSucceeded.ShouldBeTrue();
    [Fact] void should_reject_second_request() => Context.SecondResultSucceeded.ShouldBeFalse();
    [Fact] void should_report_already_completed() => Context.SecondFailureName.ShouldEqual("AlreadyCompleted");
}
