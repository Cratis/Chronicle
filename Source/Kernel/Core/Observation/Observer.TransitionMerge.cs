// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;
using Cratis.Orleans.StateMachines;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Keeps the positions, in-flight partitions and handled-event totals recorded while a state transition is awaiting.
/// </summary>
/// <remarks>
/// The state machine replaces <see cref="ObserverState"/> with whatever a state's <c language="csharp">OnEnter</c> or <c language="csharp">OnLeave</c>
/// returns, and those derive their result from the snapshot they were handed before awaiting. Handling, handled-count
/// flushes and partition replays run while such a hook awaits - a transition started by an interleaving catch-up
/// report does not block them - so without this every in-flight marker cleared and every handled count added in that
/// window is overwritten by the snapshot. The marker then stays persisted for good and the totals fall short (#4972).
/// Position ownership is resolved per field, recovery relinquishes markers by set difference, and replay owns its reset.
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
    /// Notes that positions, in-flight partitions or handled-event totals changed during an awaiting transition hook.
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

        State = ObserverTransitionStateMerge.Merge(handed, recorded, State);
    }
}
