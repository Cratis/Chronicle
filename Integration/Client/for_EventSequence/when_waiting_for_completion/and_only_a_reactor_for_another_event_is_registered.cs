// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_waiting_for_completion.and_only_a_reactor_for_another_event_is_registered.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_waiting_for_completion;

[Collection(ChronicleCollection.Name)]
public class and_only_a_reactor_for_another_event_is_registered(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public AppendResultWaitForCompletionResult Result { get; private set; }

        public override IEnumerable<Type> EventTypes => [typeof(ARecorded), typeof(BRecorded)];
        public override IEnumerable<Type> Reactors => [typeof(BReactor)];

        async Task Because()
        {
            await EventStore.Reactors.GetHandlerFor<BReactor>().WaitTillSubscribed();
            var appendResult = await EventStore.EventLog.Append("source", new ARecorded());
            Result = await appendResult.WaitForCompletion(TimeSpan.FromSeconds(5));
        }
    }

    [Fact] void should_complete_without_waiting_for_the_unrelated_reactor() => Context.Result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_time_out() => Context.Result.TimedOut.ShouldBeFalse();
}
