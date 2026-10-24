// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Sequences.for_AppendCompletion;

public class when_the_silo_becomes_unavailable : Specification
{
    readonly CorrelationId _correlationId = Guid.NewGuid();
    AppendResult _single;
    AppendManyResult _batch;

    async Task Because()
    {
        _single = await Task.FromException<AppendResult>(new SiloUnavailableException()).WithUnknownOutcome(_correlationId);
        _batch = await Task.FromException<AppendManyResult>(new SiloUnavailableException()).WithUnknownOutcome(_correlationId);
    }

    [Fact] void should_report_the_single_append_as_unknown() => _single.Errors.ShouldContainOnly([AppendError.AppendOutcomeUnknown]);
    [Fact] void should_report_the_batch_as_unknown() => _batch.Errors.ShouldContainOnly([AppendError.AppendOutcomeUnknown]);
    [Fact] void should_preserve_the_single_correlation_id() => _single.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_preserve_the_batch_correlation_id() => _batch.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_not_claim_the_single_append_succeeded() => _single.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_claim_the_batch_succeeded() => _batch.IsSuccess.ShouldBeFalse();
}
