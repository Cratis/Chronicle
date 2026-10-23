// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Sequences.for_AppendCompletion;

public class when_an_append_is_rejected : Specification
{
    readonly AppendResult _rejection = new() { Errors = [AppendError.PublicEventTypeCannotBeAppendedToEventLog] };
    readonly AppendManyResult _batchRejection = new() { Errors = [AppendError.ConstraintIndexUnavailable] };
    AppendResult _single;
    AppendManyResult _batch;

    async Task Because()
    {
        _single = await Task.FromResult(_rejection).WithUnknownOutcome(Guid.NewGuid());
        _batch = await Task.FromResult(_batchRejection).WithUnknownOutcome(Guid.NewGuid());
    }

    [Fact] void should_preserve_the_single_rejection() => _single.ShouldBeSame(_rejection);
    [Fact] void should_preserve_the_batch_rejection() => _batch.ShouldBeSame(_batchRejection);
}
