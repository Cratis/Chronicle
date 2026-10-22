// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Sequences.for_AppendCompletion;

public class when_the_grain_call_times_out : Specification
{
    AppendResult _single;
    AppendManyResult _batch;

    async Task Because()
    {
        _single = await Task.FromException<AppendResult>(new TimeoutException()).WithUnknownOutcome(Guid.NewGuid());
        _batch = await Task.FromException<AppendManyResult>(new TimeoutException()).WithUnknownOutcome(Guid.NewGuid());
    }

    [Fact] void should_report_the_single_append_as_unknown() => _single.Errors.ShouldContainOnly([AppendError.AppendOutcomeUnknown]);
    [Fact] void should_report_the_batch_as_unknown() => _batch.Errors.ShouldContainOnly([AppendError.AppendOutcomeUnknown]);
}
