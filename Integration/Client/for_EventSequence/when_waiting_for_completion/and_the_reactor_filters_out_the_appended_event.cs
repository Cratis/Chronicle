// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.for_Reactors;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_waiting_for_completion.and_the_reactor_filters_out_the_appended_event.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_waiting_for_completion;

[Collection(ChronicleCollection.Name)]
public class and_the_reactor_filters_out_the_appended_event(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public AppendResultWaitForCompletionResult Result { get; private set; }

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];
        public override IEnumerable<Type> Reactors => [typeof(ReactorFilteredByTag)];

        protected override void ConfigureServices(IServiceCollection services) =>
            services.AddSingleton(new ReactorFilteredByTag(new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)));

        async Task Because()
        {
            await EventStore.Reactors.GetHandlerFor<ReactorFilteredByTag>().WaitTillSubscribed();
            var appendResult = await EventStore.EventLog.Append("source", new SomeEvent(1));
            Result = await appendResult.WaitForCompletion(TimeSpan.FromSeconds(5));
        }
    }

    [Fact] void should_complete_without_waiting_for_the_filtered_reactor() => Context.Result.IsSuccess.ShouldBeTrue();
}
