// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.XUnit.Integration.for_KernelFailureGuard;

public class when_catching_the_public_failure : Specification
{
    KernelProcessExited? _caught;

    async Task Because()
    {
        var guard = new KernelFailureGuard(
            "kernel",
            _ => Task.FromResult<long?>(137),
            _ => Task.FromResult("Out of memory."),
            _ => Task.CompletedTask);
        try
        {
            await guard.Run(_ => Task.CompletedTask, () => true);
        }
        catch (KernelProcessExited exception)
        {
            _caught = exception;
        }
    }

    [Fact] void should_be_catchable_by_type() => _caught.ShouldNotBeNull();
    [Fact] void should_be_accessible_to_downstream_callers() => typeof(KernelProcessExited).IsPublic.ShouldBeTrue();
    [Fact] void should_not_expose_a_public_constructor() => typeof(KernelProcessExited).GetConstructors().ShouldBeEmpty();
    [Fact] void should_report_the_confirmed_exit() => _caught!.Message.ShouldContain("137: Out of memory.");
}
