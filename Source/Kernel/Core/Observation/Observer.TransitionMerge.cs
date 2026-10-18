// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Orleans.StateMachines;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Keeps the in-flight partitions and handled-event totals that handling records while a state transition is awaiting.
/// </summary>
/// <remarks>
/// The state machine replaces <see cref="ObserverState"/> with whatever a state's <c language="csharp">OnEnter</c> or <c language="csharp">OnLeave</c>
/// returns, and those derive their result from the snapshot they were handed before awaiting. Handling, handled-count
/// flushes and partition replays run while such a hook awaits - a transition started by an interleaving catch-up
/// report does not block them - so without this every in-flight marker cleared and every handled count added in that
/// window is overwritten by the snapshot. The marker then stays persisted for good and the totals fall short (#4972).
/// No state owns these fields, so a hook that left them as it found them takes what handling recorded meanwhile.
/// </remarks>
public partial class Observer
{
    ObserverState? _stateHandedToTransitionHook;
    ObserverState? _stateRecordedDuringTransitionHook;

    /// <inheritdoc/>
    protected override Task OnBeforeLeavingState(IState<ObserverState> state)
    {
        BeginTransitionHook();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override Task OnAfterLeavingState(IState<ObserverState> state)
    {
        KeepWhatWasRecordedDuringTransitionHook();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Determines whether the transition hook decided the handled-event totals itself.
    /// </summary>
    /// <remarks>
    /// The totals are the hook's when they differ from the handed snapshot by value, or when the hook started a replay.
    /// Reference equality is not enough: with zero handed the dictionary is the shared empty singleton, so a replay's
    /// reset (a zero count and an empty dictionary) is indistinguishable from the hook having left them alone, and counts
    /// recorded meanwhile would wrongly survive the reset. Starting a replay is the one transition that resets the
    /// totals, so entering it is what marks them as the hook's even when the reset changed nothing by value.
    /// </remarks>
    /// <param name="handed">The state the hook was handed.</param>
    /// <param name="returned">The state the hook returned.</param>
    /// <returns>True if the hook owns the totals, false if not.</returns>
    static bool HookOwnsHandledCounts(ObserverState handed, ObserverState returned) =>
        (returned.IsReplaying && !handed.IsReplaying) ||
        returned.HandledEventCount != handed.HandledEventCount ||
        !HasSameCounts(returned.HandledEventCountPerEventType, handed.HandledEventCountPerEventType);

    static bool HasSameCounts(IReadOnlyDictionary<EventTypeId, EventCount> left, IReadOnlyDictionary<EventTypeId, EventCount> right) =>
        ReferenceEquals(left, right) ||
        (left.Count == right.Count && left.All(_ => right.TryGetValue(_.Key, out var count) && count == _.Value));

    /// <summary>
    /// Remembers the state a transition hook is about to be handed.
    /// </summary>
    /// <remarks>
    /// The hook reads <see cref="ObserverState"/> from the state machine's continuation after <c language="csharp">_alertMutationLock</c>,
    /// so the snapshot taken here is the one it is handed only while that continuation runs inline; nothing else is awaited
    /// between this and the hook reading it.
    /// </remarks>
    void BeginTransitionHook()
    {
        _stateHandedToTransitionHook = State;
        _stateRecordedDuringTransitionHook = null;
    }

    /// <summary>
    /// Notes that in-flight partitions or handled-event totals changed, so a transition hook awaiting meanwhile keeps them.
    /// </summary>
    void RecordedHandlingState()
    {
        if (_stateHandedToTransitionHook is not null)
        {
            _stateRecordedDuringTransitionHook = State;
        }
    }

    /// <summary>
    /// Carries what handling recorded during a transition hook over to the state the hook returned.
    /// </summary>
    /// <remarks>
    /// A field the hook itself changed - such as the totals a replay resets - is the hook's to decide and is kept.
    /// </remarks>
    void KeepWhatWasRecordedDuringTransitionHook()
    {
        var handed = _stateHandedToTransitionHook;
        var recorded = _stateRecordedDuringTransitionHook;
        _stateHandedToTransitionHook = null;
        _stateRecordedDuringTransitionHook = null;
        if (handed is null || recorded is null)
        {
            return;
        }

        var returned = State;
        if (ReferenceEquals(returned.InFlightPartitions, handed.InFlightPartitions))
        {
            returned = returned with { InFlightPartitions = recorded.InFlightPartitions };
        }

        if (!HookOwnsHandledCounts(handed, returned))
        {
            returned = returned with
            {
                HandledEventCount = recorded.HandledEventCount,
                HandledEventCountPerEventType = recorded.HandledEventCountPerEventType
            };
        }

        State = returned;
    }
}
