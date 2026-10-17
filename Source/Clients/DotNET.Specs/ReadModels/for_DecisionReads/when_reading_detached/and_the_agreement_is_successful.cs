// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_DecisionReads.when_reading_detached;

public class and_the_agreement_is_successful : given.a_decision_reader
{
    async Task Because()
    {
        await _reader.GetDetached<Model>("source");
        await _reader.GetDetached<Model>("source");
    }

    [Fact] void should_cache_the_successful_agreement() => _projectionService.ReceivedCalls().Count().ShouldEqual(1);
    [Fact] void should_fold_each_read() => _folds.ShouldEqual(2);
}
