// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_appending.with_closing_event.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending;

[Collection(ChronicleCollection.Name)]
public class with_closing_event(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.an_event_log_with_closing_streams(fixture)
    {
        public IAppendResult Closing;
        public IAppendResult Following;
        public bool Completed;

        async Task Because()
        {
            Closing = await Append(new given.StreamClosed());
            Completed = await EventStore.EventLog.IsStreamCompleted(Scope);
            Following = await Append(new given.StreamActivity("after close"));
        }
    }

    [Fact] void should_accept_the_closing_fact() => Context.Closing.IsSuccess.ShouldBeTrue();
    [Fact] void should_complete_the_scope() => Context.Completed.ShouldBeTrue();
    [Fact] void should_refuse_a_later_covered_fact() => Context.Following.HasConstraintViolations.ShouldBeTrue();
}
