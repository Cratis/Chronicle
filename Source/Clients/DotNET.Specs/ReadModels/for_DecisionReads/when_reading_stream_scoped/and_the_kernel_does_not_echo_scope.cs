// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_DecisionReads.when_reading_stream_scoped;

public class and_the_kernel_does_not_echo_scope : given.a_decision_reader
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => _reader.GetDetached<Model>("source", "stream-type", "stream-id"));

    [Fact] void should_refuse_the_unguarded_fold() => ((DecisionReadRefused)_error).Reason.ShouldEqual(DecisionReadRefusalReason.StreamScopeNotSupported);
    [Fact] void should_not_retry_the_unsupported_read() => _folds.ShouldEqual(1);
}
