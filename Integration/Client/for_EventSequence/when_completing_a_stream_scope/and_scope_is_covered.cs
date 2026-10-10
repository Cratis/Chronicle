// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_completing_a_stream_scope.and_scope_is_covered.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_completing_a_stream_scope;

[Collection(ChronicleCollection.Name)]
public class and_scope_is_covered(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.an_event_log_with_closing_streams(fixture)
    {
        public bool Succeeded;
        public string Error;
        public int ClosureCount;

        async Task Establish() => await EventStore.EventLog.CompleteStream(ClosedStreamScope.ForEventSource(Source));

        async Task Because()
        {
            var result = await EventStore.EventLog.CompleteStream(Scope);
            Succeeded = result.IsSuccess;
            Error = result.TryGetError(out var error) ? error.ToString() : string.Empty;
            ClosureCount = (await EventStore.EventLog.GetClosedStreams()).Count;
        }
    }

    [Fact] void should_not_complete_again() => Context.Succeeded.ShouldBeFalse();
    [Fact] void should_report_already_completed() => Context.Error.ShouldEqual("AlreadyCompleted");
    [Fact] void should_not_write_another_closure() => Context.ClosureCount.ShouldEqual(1);
}
