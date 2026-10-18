// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendResultConverters;

public class when_converting_a_batch_response_with_null_diagnostics : Specification
{
    AppendManyResult _result;

    void Because() => _result = new Contracts.Sequences.AppendManyResponse
    {
        SequenceNumbers = [42],
        ConstraintViolations = null!,
        Errors = null!,
        ConcurrencyViolations = null!
    }.ToClient();

    [Fact] void should_normalize_constraint_violations() => _result.ConstraintViolations.ShouldBeEmpty();
    [Fact] void should_normalize_errors() => _result.Errors.ShouldBeEmpty();
    [Fact] void should_normalize_concurrency_violations() => _result.ConcurrencyViolations.ShouldBeEmpty();
    [Fact] void should_preserve_the_durable_position() => _result.SequenceNumbers.Single().Value.ShouldEqual(42UL);
}
