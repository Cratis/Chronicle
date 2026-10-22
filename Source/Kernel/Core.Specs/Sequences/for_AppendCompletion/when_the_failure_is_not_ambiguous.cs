// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Sequences.for_AppendCompletion;

public class when_the_failure_is_not_ambiguous : Specification
{
    readonly Exception _ordinary = new InvalidOperationException("ordinary failure");
    readonly Exception _cancellation = new OperationCanceledException();
    Exception _ordinaryResult;
    Exception _cancellationResult;

    async Task Because()
    {
        _ordinaryResult = await Catch.Exception(() => Task.FromException<AppendResult>(_ordinary).WithUnknownOutcome(Guid.NewGuid()));
        _cancellationResult = await Catch.Exception(() => Task.FromException<AppendManyResult>(_cancellation).WithUnknownOutcome(Guid.NewGuid()));
    }

    [Fact] void should_preserve_ordinary_failures() => _ordinaryResult.ShouldBeSame(_ordinary);
    [Fact] void should_preserve_cancellation() => _cancellationResult.ShouldBeSame(_cancellation);
}
