// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Integration.SqlClustering.for_ClusterLoad;

public class when_classifying_returned_appends : Specification
{
    [Fact]
    public void should_not_classify_success_as_unknown() =>
        ClusterLoad.IsUncertainAppend(new AppendResult()).ShouldBeFalse();

    [Fact]
    public void should_classify_the_typed_unknown_outcome_as_unknown() =>
        ClusterLoad.IsUncertainAppend(new AppendResult { Errors = [AppendError.AppendOutcomeUnknown] }).ShouldBeTrue();

    [Fact]
    public void should_not_classify_a_definite_failure_as_unknown() =>
        ClusterLoad.IsUncertainAppend(new AppendResult { Errors = [AppendError.Unknown] }).ShouldBeFalse();

    [Fact]
    public void should_not_classify_an_error_merely_mentioning_unknown_outcome_as_unknown() =>
        ClusterLoad.IsUncertainAppend(new AppendResult { Errors = [new AppendError("Other failure: AppendOutcomeUnknown")] }).ShouldBeFalse();
}
