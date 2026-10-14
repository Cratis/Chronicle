// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.for_ObservationOnlyFailures;

/// <summary>
/// Observation-only code contains its own failures, but never a failure that means the process itself is broken.
/// </summary>
public class when_checking_whether_a_failure_can_be_contained : Specification
{
    [Fact] void should_contain_an_ordinary_failure() => ObservationOnlyFailures.CanBeContained(new SomethingWentWrong()).ShouldBeTrue();
    [Fact] void should_not_contain_running_out_of_memory() => ObservationOnlyFailures.CanBeContained(new OutOfMemoryException()).ShouldBeFalse();
    [Fact] void should_not_contain_a_stack_overflow() => ObservationOnlyFailures.CanBeContained(new StackOverflowException()).ShouldBeFalse();
    [Fact] void should_not_contain_an_access_violation() => ObservationOnlyFailures.CanBeContained(new AccessViolationException()).ShouldBeFalse();
    [Fact] void should_not_contain_insufficient_execution_stack() => ObservationOnlyFailures.CanBeContained(new InsufficientExecutionStackException()).ShouldBeFalse();

    /// <summary>
    /// The exception that is thrown when observation-only code in this specification fails.
    /// </summary>
    public class SomethingWentWrong() : Exception("Something went wrong");
}
