// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.InMemory.for_InMemoryStorageErrorClassifier;

public class when_classifying_failures : Specification
{
    readonly InMemoryStorageErrorClassifier _classifier = new();

    [Fact] void should_retry_cancellation() => _classifier.IsTransient(new OperationCanceledException()).ShouldBeTrue();
    [Fact] void should_retry_timeouts() => _classifier.IsTransient(new TimeoutException()).ShouldBeTrue();
    [Fact] void should_retry_nested_transient_errors() => _classifier.IsTransient(new InvalidOperationException("outer", new TimeoutException())).ShouldBeTrue();
    [Fact] void should_inspect_all_aggregate_errors() => _classifier.IsTransient(new AggregateException(new FormatException(), new TimeoutException())).ShouldBeTrue();
    [Fact] void should_not_retry_deterministic_errors() => _classifier.IsTransient(new FormatException()).ShouldBeFalse();
}
