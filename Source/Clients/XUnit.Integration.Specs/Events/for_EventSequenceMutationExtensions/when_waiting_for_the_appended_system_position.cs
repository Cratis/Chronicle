// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;
using KernelAppendResult = KernelCore::Cratis.Chronicle.EventSequences.AppendResult;

namespace Cratis.Chronicle.XUnit.Integration.Events.for_EventSequenceMutationExtensions;

public class when_waiting_for_the_appended_system_position : Specification
{
    const ulong LogPosition = 5;
    const ulong SystemPosition = 17;
    readonly IReactors _reactors = Substitute.For<IReactors>();
    readonly IReactorHandler _handler = Substitute.For<IReactorHandler>();
    readonly TaskCompletionSource _polling = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<ReactorState> _checkpoint = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _completedBeforeCheckpoint;
    int _appends;

    void Establish()
    {
        ReactorId id = "$system.Cratis.Chronicle.EventSequences.EventSequencesReactor";
        _reactors.GetHandlerById(id).Returns(_handler);
        _handler.Id.Returns(id);
        _handler.GetState().Returns(
            _ => Task.FromResult(new ReactorState(id, ObserverRunningState.Active, true, SystemPosition, LogPosition, SystemPosition)),
            _ =>
            {
                _polling.TrySetResult();
                return _checkpoint.Task;
            });
    }

    async Task Because()
    {
        var waiting = EventSequenceMutationExtensions.AppendAndWait(Append, _reactors, $"revise Log/{LogPosition}");
        await Task.WhenAny(_polling.Task, waiting);
        _completedBeforeCheckpoint = waiting.IsCompleted;
        _checkpoint.SetResult(new ReactorState(_handler.Id, ObserverRunningState.Active, true, SystemPosition + 1, SystemPosition, SystemPosition));
        await waiting;
    }

    Task<KernelAppendResult> Append()
    {
        _appends++;
        return Task.FromResult(new KernelAppendResult { SequenceNumber = SystemPosition });
    }

    [Fact] void should_submit_one_system_request() => _appends.ShouldEqual(1);
    [Fact] void should_not_treat_the_log_position_or_system_tail_as_acknowledgment() => _completedBeforeCheckpoint.ShouldBeFalse();
    [Fact] Task should_check_for_partition_failure_after_the_checkpoint() => _handler.Received(1).GetFailedPartitions();
}
