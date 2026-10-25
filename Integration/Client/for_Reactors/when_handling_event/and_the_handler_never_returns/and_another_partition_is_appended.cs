// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;
using context = Cratis.Chronicle.Integration.for_Reactors.when_handling_event.and_the_handler_never_returns.and_another_partition_is_appended.context;

namespace Cratis.Chronicle.Integration.for_Reactors.when_handling_event.and_the_handler_never_returns;

[Collection(ChronicleCollection.Name)]
public class and_another_partition_is_appended(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    {
        public EventSourceId HangingSource = "hanging-source";
        public EventSourceId OtherSource = "other-source";
        public ReactorWithHangingHandler Reactor;
        public IEnumerable<FailedPartition> FailedPartitions;
        public ReactorState ReactorState;
        public EventSourceId HandledSource;
        public TimeSpan TimeToFailure;
        public bool HandlerStillHanging;
        public static readonly TimeSpan Deadline = TimeSpan.FromSeconds(60);
        readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override IEnumerable<Type> Reactors => [typeof(ReactorWithHangingHandler)];
        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];

        protected override void ConfigureServices(IServiceCollection services)
        {
            Reactor = new ReactorWithHangingHandler(HangingSource, _release.Task);
            services.AddSingleton(Reactor);
        }

        async Task Establish()
        {
            await EventStore.Reactors.GetHandlerFor<ReactorWithHangingHandler>().WaitTillSubscribed(Deadline).WaitAsync(Deadline);
        }

        async Task Because()
        {
            var reactor = EventStore.Reactors.GetHandlerFor<ReactorWithHangingHandler>();
            using var deadline = new CancellationTokenSource(Deadline);
            var stopwatch = Stopwatch.StartNew();
            await EventStore.EventLog.Append(HangingSource, new SomeEvent(42)).WaitAsync(deadline.Token);
            await Reactor.HandlerEntered.Task.WaitAsync(deadline.Token);
            await EventStore.EventLog.Append(OtherSource, new SomeEvent(43)).WaitAsync(deadline.Token);

            FailedPartitions = await reactor.WaitForThereToBeFailedPartitions(Deadline).WaitAsync(deadline.Token);
            TimeToFailure = stopwatch.Elapsed;
            Console.WriteLine($"Hanging reactor partition failed after {TimeToFailure.TotalSeconds:F3} seconds.");
            HandledSource = await Reactor.HandledSource.Task.WaitAsync(deadline.Token);
            HandlerStillHanging = !Reactor.HangingHandler.IsCompleted;
            ReactorState = await reactor.GetState().WaitAsync(deadline.Token);
        }

        async Task Destroy()
        {
            _release.TrySetResult();
            await Reactor.HangingHandler.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }

    [Fact] void should_fail_only_the_hanging_partition() => Context.FailedPartitions.Select(_ => _.Partition.Value).ShouldEqual([Context.HangingSource.Value]);
    [Fact] void should_classify_the_failure_as_a_timeout() => Context.FailedPartitions.Single().Attempts.First().Kind.ShouldEqual(FailureKind.Timeout);
    [Fact] void should_detect_the_failure_within_the_bound() => (Context.TimeToFailure <= context.Deadline).ShouldBeTrue();
    [Fact] void should_handle_the_other_partition() => Context.HandledSource.ShouldEqual(Context.OtherSource);
    [Fact] void should_not_need_the_hanging_handler_to_return() => Context.HandlerStillHanging.ShouldBeTrue();
    [Fact] void should_keep_the_observer_active() => Context.ReactorState.RunningState.ShouldEqual(ObserverRunningState.Active);
}
