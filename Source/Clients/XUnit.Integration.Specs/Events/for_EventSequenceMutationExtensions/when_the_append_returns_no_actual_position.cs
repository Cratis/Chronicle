// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;
using Cratis.Chronicle.Reactors;
using KernelAppendError = KernelCore::Cratis.Chronicle.EventSequences.AppendError;
using KernelAppendResult = KernelCore::Cratis.Chronicle.EventSequences.AppendResult;

namespace Cratis.Chronicle.XUnit.Integration.Events.for_EventSequenceMutationExtensions;

public class when_the_append_returns_no_actual_position : Specification
{
    readonly IReactors _reactors = Substitute.For<IReactors>();
    InvalidOperationException? _missingPosition;
    InvalidOperationException? _rejectedAppend;

    async Task Because()
    {
        _missingPosition = await Record.ExceptionAsync(() => WaitFor(new KernelAppendResult())) as InvalidOperationException;
        _rejectedAppend = await Record.ExceptionAsync(() => WaitFor(new KernelAppendResult { Errors = [KernelAppendError.ConstraintIndexUnavailable] })) as InvalidOperationException;
    }

    Task WaitFor(KernelAppendResult result) => EventSequenceMutationExtensions.AppendAndWait(() => Task.FromResult(result), _reactors, "revise Log/5");

    [Fact] void should_reject_the_missing_checkpoint() => _missingPosition.ShouldNotBeNull();
    [Fact] void should_report_an_append_rejection() => _rejectedAppend.ShouldNotBeNull();
    [Fact] void should_not_look_up_the_handler() => _reactors.DidNotReceive().GetHandlerById(Arg.Any<ReactorId>());
}
