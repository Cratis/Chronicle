// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Extension methods for classifying what an <see cref="Exception"/> from a subscriber call says about the failure.
/// </summary>
public static class ExceptionFailureKindExtensions
{
    /// <summary>
    /// Classify what kind of thing an exception from a subscriber call represents.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to classify.</param>
    /// <returns>The <see cref="FailureKind"/> the exception represents.</returns>
    /// <remarks>
    /// A request that outlives its response timeout surfaces as a <see cref="TimeoutException"/>, and says the
    /// subscriber never got to answer rather than that it answered wrongly. Everything else is taken at face value as
    /// the subscriber failing to handle the events. The whole exception chain is searched, because a timeout that
    /// travels back through another call arrives wrapped.
    /// </remarks>
    public static FailureKind ToFailureKind(this Exception exception) =>
        HasTimeout(exception) ? FailureKind.Timeout : FailureKind.Handling;

    static bool HasTimeout(Exception? exception) => exception switch
    {
        null => false,
        TimeoutException => true,
        AggregateException aggregate => aggregate.InnerExceptions.Any(HasTimeout),
        _ => HasTimeout(exception.InnerException)
    };
}
