// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Integration.for_AccumulatorRedeliverySafety.given;

/// <summary>
/// Drives an accumulating projection, a collapsing projection and a running-total reducer through a catch-up that
/// is interrupted by a kernel crash, which is the only fault that reproduces the redelivery window a debounced
/// progress checkpoint leaves open.
/// </summary>
/// <param name="chronicleFixture">The <see cref="ChronicleFixture"/>.</param>
/// <remarks>
/// Three things make or break this scenario, and each of them turns it green while proving nothing when it is
/// wrong.
/// <list type="number">
/// <item>
/// It has to be a <b>catch-up</b>, not a replay. A replay is dispatched as a single step over the whole sequence
/// and resets the sink through <c>BeginReplay</c>, so the accumulator starts from zero and lands on the right
/// answer no matter how much was redelivered. The events are therefore appended <b>before</b> the observers are
/// waited on, so the observers reach the tail through catch-up.
/// </item>
/// <item>
/// It has to kill the <b>kernel</b>, not storage. Restarting storage keeps every durable write and every grain's
/// in-memory state, which is why the existing restart scenario has never surfaced this.
/// </item>
/// <item>
/// It has to assert that something was actually redelivered. <see cref="RedeliveredSequenceNumbers"/> is that
/// evidence; without it a green run cannot be told apart from a run where the crash landed on a checkpoint
/// boundary and nothing was redelivered at all.
/// </item>
/// </list>
/// The number of events is deliberately several times the default
/// <c>Jobs.StepCheckpointBatchInterval</c> so the crash is very likely to land between two durable checkpoints.
/// </remarks>
public class accumulators_catching_up_across_a_kernel_crash(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
{
    /// <summary>
    /// The number of events appended for the single partition under test.
    /// </summary>
    public const int EventCount = 500;

    /// <summary>
    /// The event source every event is appended to.
    /// </summary>
    public static readonly EventSourceId TheEventSource = "accumulator-source";

    /// <inheritdoc/>
    public override IEnumerable<Type> EventTypes => [typeof(AmountRecorded)];

    /// <inheritdoc/>
    public override IEnumerable<Type> Projections => [typeof(CountingAmountsProjection), typeof(CollapsingCountProjection)];

    /// <inheritdoc/>
    public override IEnumerable<Type> Reducers => [typeof(RunningTotalReducer)];

    /// <summary>
    /// Gets the reducer instance, which records every sequence number it is handed.
    /// </summary>
    public RunningTotalReducer Reducer { get; private set; }

    /// <summary>
    /// Gets the sequence number of the last appended event.
    /// </summary>
    public EventSequenceNumber LastEventSequenceNumber { get; private set; }

    /// <summary>
    /// Gets the number of sequence numbers the reducer saw more than once.
    /// </summary>
    public int RedeliveredSequenceNumbers { get; private set; }

    /// <summary>
    /// Gets the counting projection's read model after the observers reached the tail.
    /// </summary>
    public CountedAmounts CountedResult { get; private set; }

    /// <summary>
    /// Gets the collapsing projection's read model after the observers reached the tail.
    /// </summary>
    public CollapsedCount CollapsedResult { get; private set; }

    /// <summary>
    /// Gets the running total after the observers reached the tail.
    /// </summary>
    public RunningTotal TotalResult { get; private set; }

    async Task Because()
    {
        for (var index = 0; index < EventCount; index++)
        {
            var result = await EventStore.EventLog.Append(TheEventSource, new AmountRecorded(1));
            LastEventSequenceNumber = result.SequenceNumber;
        }

        await ChronicleFixture.RestartKernelAsync();

        var reducerHandler = EventStore.Reducers.GetHandlerFor<RunningTotalReducer>();
        await reducerHandler.WaitTillSubscribed(TimeSpanFactory.FromSeconds(60));
        await reducerHandler.WaitTillReachesEventSequenceNumber(LastEventSequenceNumber, TimeSpanFactory.FromSeconds(120));

        var countingProjection = EventStore.Projections.GetHandlerFor<CountingAmountsProjection>();
        await countingProjection.WaitTillReachesEventSequenceNumber(LastEventSequenceNumber, TimeSpanFactory.FromSeconds(120));

        var collapsingProjection = EventStore.Projections.GetHandlerFor<CollapsingCountProjection>();
        await collapsingProjection.WaitTillReachesEventSequenceNumber(LastEventSequenceNumber, TimeSpanFactory.FromSeconds(120));

        RedeliveredSequenceNumbers = Reducer.RedeliveredSequenceNumberCount;
        CountedResult = await EventStore.ReadModels.GetInstanceById<CountedAmounts>(TheEventSource.ToString());
        CollapsedResult = await EventStore.ReadModels.GetInstanceById<CollapsedCount>(CollapsingCountProjection.ConstantKeyValue);
        TotalResult = await EventStore.ReadModels.GetInstanceById<RunningTotal>(TheEventSource.ToString());
    }

    /// <inheritdoc/>
    protected override void ConfigureServices(IServiceCollection services)
    {
        Reducer = new RunningTotalReducer();
        services.AddSingleton(Reducer);
    }
}
