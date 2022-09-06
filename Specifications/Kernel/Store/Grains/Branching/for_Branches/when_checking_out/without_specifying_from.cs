// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store.Branching;
using Aksio.Cratis.Execution;
using Orleans;

namespace Aksio.Cratis.Events.Store.Grains.Branching.for_Branches.when_checking_out;

public class without_specifying_from : GrainSpecification<BranchesState>
{
    static EventSequenceNumber sequence_number = 42;
    protected Mock<IExecutionContextManager> execution_context_manager;
    protected ExecutionContext execution_context;
    protected Mock<IEventSequence> event_sequence;
    protected Branches branches;

    protected override Grain GetGrainInstance()
    {
        execution_context_manager = new();
        execution_context = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            CorrelationId.New(),
            CausationId.System,
            CausedBy.System
        );

        execution_context_manager.SetupGet(_ => _.Current).Returns(execution_context);

        return branches = new Branches(execution_context_manager.Object);
    }

    protected override void OnBeforeGrainActivate()
    {
        event_sequence = new();
        grain_factory.Setup(_ => _.GetGrain<IEventSequence>(EventSequenceId.Log, IsAny<string>(), null)).Returns(event_sequence.Object);
    }

    void Establish()
    {
        event_sequence.Setup(_ => _.GetTailSequenceNumber()).Returns(Task.FromResult(sequence_number));
    }

    Task Because() => branches.Checkout(BranchTypeId.NotSpecified);

    [Fact] void should_hold_one_branch() => state.Branches.Count.ShouldEqual(1);
    [Fact] void should_use_tail_sequence_as_from() => state.Branches[0].From.ShouldEqual(sequence_number);
}
