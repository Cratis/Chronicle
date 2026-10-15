// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reducers;

using context = Cratis.Chronicle.Integration.for_Reducers.when_using_self_reducers.and_failed_replay_does_not_promote_and_can_be_repaired.context;

namespace Cratis.Chronicle.Integration.for_Reducers.when_using_self_reducers;

[Collection(ChronicleCollection.Name)]
public class and_failed_replay_does_not_promote_and_can_be_repaired(context setup) : Given<context>(setup)
{
    public class context(ChronicleFixture fixture) : Specification<ChronicleFixture>(fixture)
    {
        public int[] BeforeRepair;
        public int[] AfterRepair;
        public bool BothHaveFailedPartitions;
        public override IEnumerable<Type> EventTypes => [typeof(Added)];
        public override IEnumerable<Type> Reducers => [typeof(SelfTotal), typeof(Classic)];

        async Task Because()
        {
            var handlers = new[] { EventStore.Reducers.GetHandlerFor(typeof(SelfTotal)), EventStore.Reducers.GetHandlerFor<Classic>() };
            foreach (var handler in handlers) await handler.WaitTillSubscribed();
            (await (await EventStore.EventLog.Append("good", new Added(6, false))).WaitForCompletion()).IsSuccess.ShouldBeTrue();
            var bad = await EventStore.EventLog.Append("bad", new Added(5, true));
            (await bad.WaitForCompletion()).IsSuccess.ShouldBeTrue();
            foreach (var handler in handlers) await Replay(handler);
            BothHaveFailedPartitions = (await handlers[0].GetFailedPartitions()).Any() && (await handlers[1].GetFailedPartitions()).Any();
            BeforeRepair = await ReadTotals();

            // Repair the persisted input, not the invoker or kernel's failure semantics.
            await EventStore.EventLog.Revise(bad.SequenceNumber, new Added(5, false));
            foreach (var handler in handlers) await Replay(handler);
            AfterRepair = await ReadTotals();
            foreach (var handler in handlers) (await handler.GetFailedPartitions()).ShouldBeEmpty();
        }

        async Task Replay(IReducerHandler handler)
        {
            var job = await EventStore.Reducers.Replay(handler.Id);
            job.ShouldNotEqual(JobId.NotSet);
            await EventStore.Jobs.WaitTillJobCompletesOrIsDeleted(job);
            await handler.WaitTillActive();
        }

        async Task<int[]> ReadTotals() =>
        [
            (await EventStore.ReadModels.GetInstanceById<SelfTotal>("good")).Total,
            (await EventStore.ReadModels.GetInstanceById<SelfTotal>("bad")).Total,
            (await EventStore.ReadModels.GetInstanceById<ClassicTotal>("good")).Total,
            (await EventStore.ReadModels.GetInstanceById<ClassicTotal>("bad")).Total
        ];
    }

    [Fact] void should_record_the_failing_partition_for_both_forms() => Context.BothHaveFailedPartitions.ShouldBeTrue();
    [Fact] void should_keep_both_original_containers_after_failed_replay() => Context.BeforeRepair.ShouldEqual<int>([6, 5, 6, 5]);
    [Fact] void should_replay_the_revised_input_successfully_after_repair() => Context.AfterRepair.ShouldEqual<int>([60, 50, 60, 50]);

    [EventType]
    public record Added(int Amount, bool FailOnReplay);
    public record SelfTotal(int Total)
    {
        public static SelfTotal Reduce(Added e, SelfTotal? current, EventContext context) => new((current?.Total ?? 0) + Amount(e, context));
    }
    public record ClassicTotal(int Total);
    public class Classic : IReducerFor<ClassicTotal>
    {
        public ClassicTotal Apply(Added e, ClassicTotal? current, EventContext context) => new((current?.Total ?? 0) + Amount(e, context));
    }
    public class FoldFailed : Exception;
    static int Amount(Added e, EventContext context)
    {
        var replay = context.ObservationState.HasFlag(EventObservationState.Replay);
        if (replay && e.FailOnReplay) throw new FoldFailed();
        return e.Amount * (replay ? 10 : 1);
    }
}
