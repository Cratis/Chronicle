// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.EventSequences.for_AppendManyResult;

/// <summary>
/// A result built by the failure factory has to report itself as a failure. This one did not: success was
/// derived from constraint violations and errors only, so a batch rejected for a concurrency violation came
/// back saying it had succeeded - and every caller that branches on the result took the successful branch.
/// The single-event <see cref="AppendResult"/> and the client-side result both already count it.
/// </summary>
public class when_failing_with_concurrency_violations : Specification
{
    AppendManyResult _result;

    void Because() => _result = AppendManyResult.Failed(
        CorrelationId.New(),
        [new ConcurrencyViolation(EventSourceId.Unspecified, EventSequenceNumber.First, EventSequenceNumber.Unavailable)]);

    [Fact] void should_not_report_success() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_that_it_has_concurrency_violations() => _result.HasConcurrencyViolations.ShouldBeTrue();
    [Fact] void should_not_report_constraint_violations() => _result.HasConstraintViolations.ShouldBeFalse();
    [Fact] void should_not_report_errors() => _result.HasErrors.ShouldBeFalse();
}
