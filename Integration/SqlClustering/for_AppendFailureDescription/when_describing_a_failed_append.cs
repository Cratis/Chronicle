// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Integration.SqlClustering.for_AppendFailureDescription;

public class when_describing_a_failed_append : Specification
{
    string _description;

    void Because() => _description = AppendFailureDescription.Describe(new AppendResult
    {
        Errors = [new AppendError("boom")],
        ConcurrencyViolation = new ConcurrencyViolation("source-1", new EventSequenceNumber(3), new EventSequenceNumber(5)),
        IsRetryable = true
    });

    [Fact] void should_include_the_errors() => _description.ShouldContain("boom");
    [Fact] void should_include_the_concurrency_violation() => _description.ShouldContain("source-1");
    [Fact] void should_include_the_retryable_flag() => _description.ShouldContain("IsRetryable=True");
}
