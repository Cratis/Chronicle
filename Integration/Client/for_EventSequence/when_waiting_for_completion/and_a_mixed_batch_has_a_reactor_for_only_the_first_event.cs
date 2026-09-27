// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_waiting_for_completion.and_a_mixed_batch_has_a_reactor_for_only_the_first_event.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_waiting_for_completion;

[Collection(ChronicleCollection.Name)]
public class and_a_mixed_batch_has_a_reactor_for_only_the_first_event(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public AppendResultWaitForCompletionResult Result { get; private set; }

        public override IEnumerable<Type> EventTypes => [typeof(ARecorded), typeof(BRecorded)];
        public override IEnumerable<Type> Reactors => [typeof(AReactor)];

        async Task Because()
        {
            await EventStore.Reactors.GetHandlerFor<AReactor>().WaitTillSubscribed();
            var appendResult = await EventStore.EventLog.AppendMany("source", [new ARecorded(), new BRecorded()]);
            Result = await appendResult.WaitForCompletion(TimeSpan.FromSeconds(5));
        }
    }

    [Fact] void should_complete_after_the_first_event_is_handled() => Context.Result.IsSuccess.ShouldBeTrue();
}
