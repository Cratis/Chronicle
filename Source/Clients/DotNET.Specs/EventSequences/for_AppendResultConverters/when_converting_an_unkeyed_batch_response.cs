// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendResultConverters;

public class when_converting_an_unkeyed_batch_response : Specification
{
    AppendManyResult _result;

    void Because() => _result = new Contracts.Sequences.AppendManyResponse { SequenceNumbers = [1, 2] }.ToClient();

    [Fact] void should_report_every_event_as_appended() => _result.Outcomes.ToArray().ShouldEqual([AppendOutcome.Appended, AppendOutcome.Appended]);
}
