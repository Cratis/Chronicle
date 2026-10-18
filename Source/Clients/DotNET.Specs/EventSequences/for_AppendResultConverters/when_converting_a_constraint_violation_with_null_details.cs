// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendResultConverters;

public class when_converting_a_constraint_violation_with_null_details : Specification
{
    AppendResult _result;

    void Because() => _result = new Contracts.Sequences.AppendResponse
    {
        ConstraintViolations = [new() { EventTypeId = "event", ConstraintName = "unique", Message = "Already exists", Details = null! }]
    }.ToClient();

    [Fact] void should_normalize_the_details() => _result.ConstraintViolations.Single().Details.ShouldBeEmpty();
    [Fact] void should_preserve_the_violation() => _result.HasConstraintViolations.ShouldBeTrue();
    [Fact] void should_not_report_success() => _result.IsSuccess.ShouldBeFalse();
}
