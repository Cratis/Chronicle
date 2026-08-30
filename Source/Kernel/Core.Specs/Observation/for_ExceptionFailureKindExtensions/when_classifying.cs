// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_ExceptionFailureKindExtensions;

/// <summary>
/// A request that outlived its response timeout says the subscriber never got to answer, not that it answered
/// wrongly - and a timeout that travels back through another call arrives wrapped, so the whole chain counts.
/// </summary>
public class when_classifying : Specification
{
    [Fact] void should_classify_a_timeout_as_a_timeout() => new TimeoutException().ToFailureKind().ShouldEqual(FailureKind.Timeout);
    [Fact] void should_classify_a_wrapped_timeout_as_a_timeout() => new SomethingWentWrong(new TimeoutException()).ToFailureKind().ShouldEqual(FailureKind.Timeout);
    [Fact] void should_classify_a_timeout_among_aggregated_failures_as_a_timeout() => new AggregateException(new SomethingWentWrong(), new TimeoutException()).ToFailureKind().ShouldEqual(FailureKind.Timeout);
    [Fact] void should_classify_anything_else_as_handling() => new SomethingWentWrong().ToFailureKind().ShouldEqual(FailureKind.Handling);
    [Fact] void should_classify_a_wrapped_failure_as_handling() => new SomethingWentWrong(new SomethingWentWrong()).ToFailureKind().ShouldEqual(FailureKind.Handling);

    /// <summary>
    /// The exception that is thrown when a subscriber in this specification fails handling its events.
    /// </summary>
    /// <param name="innerException">The optional exception that caused it.</param>
    public class SomethingWentWrong(Exception? innerException = null) : Exception("Something went wrong", innerException);
}
