// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_completing_a_stream_scope.and_expected_tail_is_stale.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_completing_a_stream_scope;

[Collection(ChronicleCollection.Name)]
public class and_expected_tail_is_stale(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.an_event_log_with_closing_streams(fixture)
    {
        public bool Succeeded;
        public string Error;
        public bool Completed;

        async Task Establish()
        {
            await Append(new given.StreamActivity("first"));
            await Append(new given.StreamActivity("second"));
        }

        async Task Because()
        {
            var result = await EventStore.EventLog.CompleteStream(Scope, EventSequenceNumber.First);
            Succeeded = result.IsSuccess;
            Error = result.TryGetError(out var error) ? error.ToString() : string.Empty;
            Completed = await EventStore.EventLog.IsStreamCompleted(Scope);
        }
    }

    [Fact] void should_reject_the_stale_tail() => Context.Succeeded.ShouldBeFalse();
    [Fact] void should_report_tail_mismatch() => Context.Error.ShouldEqual("ExpectedTailMismatch");
    [Fact] void should_leave_the_scope_open() => Context.Completed.ShouldBeFalse();
}
