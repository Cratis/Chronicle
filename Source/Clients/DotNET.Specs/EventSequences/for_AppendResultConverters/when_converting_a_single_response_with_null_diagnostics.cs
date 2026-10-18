// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendResultConverters;

public class when_converting_a_single_response_with_null_diagnostics : Specification
{
    AppendResult _result;

    void Because() => _result = new Contracts.Sequences.AppendResponse
    {
        SequenceNumber = 42,
        ConstraintViolations = null!,
        Errors = null!
    }.ToClient();

    [Fact] void should_normalize_constraint_violations() => _result.ConstraintViolations.ShouldBeEmpty();
    [Fact] void should_normalize_errors() => _result.Errors.ShouldBeEmpty();
    [Fact] void should_preserve_the_durable_position() => _result.SequenceNumber.Value.ShouldEqual(42UL);
}
