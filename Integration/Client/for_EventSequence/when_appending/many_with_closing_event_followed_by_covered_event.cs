// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_appending.many_with_closing_event_followed_by_covered_event.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending;

[Collection(ChronicleCollection.Name)]
public class many_with_closing_event_followed_by_covered_event(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.an_event_log_with_closing_streams(fixture)
    {
        public IAppendResult Result;
        public EventSequenceNumber Tail;
        public bool Completed;

        async Task Because()
        {
            Result = await AppendMany(new given.StreamClosed(), new given.StreamActivity("covered"));
            Tail = await EventStore.EventLog.GetTailSequenceNumber();
            Completed = await EventStore.EventLog.IsStreamCompleted(Scope);
        }
    }

    [Fact] void should_reject_the_batch() => Context.Result.HasConstraintViolations.ShouldBeTrue();
    [Fact] void should_commit_no_events() => Context.Tail.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_not_commit_a_closure() => Context.Completed.ShouldBeFalse();
}
