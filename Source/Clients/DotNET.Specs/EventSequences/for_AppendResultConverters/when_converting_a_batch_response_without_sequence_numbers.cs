// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendResultConverters;

public class when_converting_a_batch_response_without_sequence_numbers : Specification
{
    AppendManyResult _result;
    Exception _error;

    void Because() => _error = Catch.Exception(() => _result = new Contracts.Sequences.AppendManyResponse { SequenceNumbers = null! }.ToClient());

    [Fact] void should_not_fabricate_a_result() => _result.ShouldBeNull();
    [Fact] void should_reject_the_missing_positions() => _error.ShouldNotBeNull();
}
