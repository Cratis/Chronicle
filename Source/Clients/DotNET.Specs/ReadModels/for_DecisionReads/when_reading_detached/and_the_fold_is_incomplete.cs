// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_DecisionReads.when_reading_detached;

public class and_the_fold_is_incomplete : given.a_decision_reader
{
    DecisionReadRefused _error;

    void Establish() => _last = 3;

    async Task Because() => _error = await Record.ExceptionAsync(() => _reader.GetDetached<Model>("source")) as DecisionReadRefused;

    [Fact] void should_refuse_the_incomplete_fold() => _error.Reason.ShouldEqual(DecisionReadRefusalReason.FoldIncomplete);
    [Fact] void should_retry_twice_before_refusing() => _folds.ShouldEqual(3);
}
