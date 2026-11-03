// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_TransientTimeouts.when_classifying_a_failed_step;

public class and_the_partial_result_is_empty : Specification
{
    readonly JsonSerializerOptions _options = new();
    JobStepResult _result;
    Exception _error;
    Exception _retryError;
    bool _transient;
    bool _retries;

    void Establish() => _result = JobStepResult.Failed(new PerformJobStepError(string.Empty, ["Handler threw"], string.Empty, false));

    void Because()
    {
        _error = Catch.Exception(() => _transient = TransientTimeouts.TryGetTransientFailure(_result, _options, out _));
        _retryError = Catch.Exception(() => _retries = TransientTimeouts.ShouldRetry(_result, 1, _options));
    }

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_not_throw_while_deciding_to_retry() => _retryError.ShouldBeNull();
    [Fact] void should_not_be_transient() => _transient.ShouldBeFalse();
    [Fact] void should_not_retry() => _retries.ShouldBeFalse();
}
