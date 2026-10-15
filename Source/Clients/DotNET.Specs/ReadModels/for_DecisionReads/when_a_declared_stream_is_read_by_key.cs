// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_DecisionReads;

public class when_a_declared_stream_is_read_by_key : given.a_declared_stream_reader
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => _reader.GetDetached<MonthlyState>("source"));

    [Fact] void should_refuse_the_read() => ((DecisionReadRefused)_error).Reason.ShouldEqual(DecisionReadRefusalReason.ReadModelRequiresStreamScope);
    [Fact] void should_refuse_key_only_admission() => _reader.Admit<MonthlyState>().Reason.ShouldEqual(DecisionReadRefusalReason.ReadModelRequiresStreamScope);
    [Fact] void should_report_the_same_reason_in_the_description() => _reader.Describe<MonthlyState>().Admission.Reason.ShouldEqual(DecisionReadRefusalReason.ReadModelRequiresStreamScope);
    [Fact] void should_not_fold() => _folds.ShouldEqual(0);
}
