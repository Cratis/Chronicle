// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Security.given;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_giving_operator_commands;

/// <summary>
/// The commands an operator gives an observer are accepted as jobs, so they return at once with the outcome of having been
/// accepted however long the observer takes to answer, and never call the observer themselves.
/// </summary>
public class and_the_observer_is_busy : given.a_replayable_observer
{
    readonly TaskCompletionSource _observerAnswers = new();
    RetryPartitionResponse _retry;
    ClearPartitionQuarantineResponse _clear;
    FastForwardResponse _fastForward;

    void Establish()
    {
        _observer.TryStartRecoverJobForFailedPartition(Arg.Any<Key>()).Returns(async _ =>
        {
            await _observerAnswers.Task;
            return Concepts.Observation.PartitionRecoveryOutcome.Started;
        });
        _observer.ReplayPartition(Arg.Any<Key>()).Returns(_observerAnswers.Task);
        _observer.ClearObserverQuarantine().Returns(_observerAnswers.Task);
    }

    async Task Because()
    {
        var timeout = TimeSpan.FromSeconds(5);
        _retry = await _observers.RetryPartition(Command<RetryPartition>("partition"), Call()).WaitAsync(timeout);
        _clear = await _observers.ClearPartitionQuarantine(Command<ClearPartitionQuarantine>("partition"), Call()).WaitAsync(timeout);
        _fastForward = await _observers.FastForward(Command<FastForward>(), Call()).WaitAsync(timeout);
        await _observers.ReplayPartition(Command<ReplayPartition>("partition"), Call()).WaitAsync(timeout);
        await _observers.ClearObserverQuarantine(Command<ClearObserverQuarantine>(), Call()).WaitAsync(timeout);
    }

    void Destroy() => _observerAnswers.TrySetResult();

    CallContext Call() => CallFor(new Probe(), CallerNamed(a_system_store_caller.OrdinaryUser));

    static TCommand Command<TCommand>(string? partition = null)
        where TCommand : IObserverCommand, new()
    {
        var command = new TCommand
        {
            EventStore = "some-event-store",
            Namespace = "some-namespace",
            ObserverId = "some-observer",
            EventSequenceId = "inbox-source"
        };
        if (partition is not null)
        {
            typeof(TCommand).GetProperty("Partition")!.SetValue(command, partition);
        }

        return command;
    }

    [Fact] void should_accept_the_retry_of_the_partition() => _retry.Outcome.ShouldEqual(PartitionRecoveryOutcome.Accepted);
    [Fact] void should_accept_clearing_the_quarantine_of_the_partition() => _clear.Outcome.ShouldEqual(ClearPartitionQuarantineOutcome.Accepted);
    [Fact] void should_accept_the_fast_forward() => _fastForward.Outcome.ShouldEqual(FastForwardOutcome.Accepted);
    [Fact] void should_accept_each_command_as_a_job() => _jobsManager.Received(5).Start<IObserverOperatorCommandJob, ObserverOperatorCommandRequest>(Arg.Any<ObserverOperatorCommandRequest>());
    [Fact] void should_not_call_the_observer() => _observer.ReceivedCalls().ShouldBeEmpty();
}
