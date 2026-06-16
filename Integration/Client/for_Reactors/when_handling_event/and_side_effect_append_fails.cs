// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;
using context = Cratis.Chronicle.Integration.for_Reactors.when_handling_event.and_side_effect_append_fails.context;

namespace Cratis.Chronicle.Integration.for_Reactors.when_handling_event;

[Collection(ChronicleCollection.Name)]
public class and_side_effect_append_fails(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    {
        public override IEnumerable<Type> Reactors => [typeof(ReactorWithFailingSideEffect)];
        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent), typeof(UniqueReactorSideEffect)];
        public override IEnumerable<Type> ConstraintTypes => [typeof(UniqueReactorSideEffectConstraint)];

        public EventSourceId EventSourceId;
        public IAppendResult SeedResult;
        public IAppendResult TriggerResult;
        public FailedPartition FailedPartition;
        public FailedPartitionAttempt FailedAttempt;

        void Establish()
        {
            EventSourceId = "side-effect-failure-source";
        }

        async Task Because()
        {
            var reactor = EventStore.Reactors.GetHandlerFor<ReactorWithFailingSideEffect>();
            await reactor.WaitTillSubscribed();

            SeedResult = await EventStore.EventLog.Append(EventSourceId, new UniqueReactorSideEffect("42"));
            TriggerResult = await EventStore.EventLog.Append(EventSourceId, new SomeEvent(42));

            var failedPartitions = await reactor.WaitForThereToBeFailedPartitions();
            FailedPartition = failedPartitions.Single();
            FailedAttempt = FailedPartition.Attempts.Single();
        }
    }

    [Fact] void should_seed_unique_side_effect_event() => Context.SeedResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_append_triggering_event() => Context.TriggerResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_fail_partition() => Context.FailedPartition.ShouldNotBeNull();
    [Fact] void should_include_constraint_violation_details() => Context.FailedAttempt.Messages.ShouldContain(_ => _.Contains("Reactor side-effect value must be unique"));
    [Fact] void should_include_side_effect_event_type() => Context.FailedAttempt.Messages.ShouldContain(_ => _.Contains(nameof(UniqueReactorSideEffect)));
    [Fact] void should_not_include_stack_trace_for_controlled_failure() => Context.FailedAttempt.StackTrace.ShouldBeEmpty();
}
