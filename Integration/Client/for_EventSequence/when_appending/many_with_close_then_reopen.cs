// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_appending.many_with_close_then_reopen.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending;

[Collection(ChronicleCollection.Name)]
public class many_with_close_then_reopen(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.an_event_log_with_closing_streams(fixture)
    {
        public IAppendResult Result;
        public bool Completed;

        async Task Because()
        {
            Result = await AppendMany(new given.StreamClosed(), new given.StreamReopened(), new given.StreamActivity("open again"));
            Completed = await EventStore.EventLog.IsStreamCompleted(Scope);
        }
    }

    [Fact] void should_accept_the_batch() => Context.Result.IsSuccess.ShouldBeTrue();
    [Fact] void should_leave_the_scope_open() => Context.Completed.ShouldBeFalse();
    [Fact] Task should_commit_all_three_facts() => Context.ShouldHaveTailSequenceNumber(2);
}
