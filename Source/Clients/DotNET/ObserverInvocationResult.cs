// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events;

namespace Cratis.Observation;

/// <summary>
/// Represents the result from invoking an observer.
/// </summary>
/// <param name="LastSuccessfullyObservedEvent">The sequence number of the last successful observed event.</param>
/// <param name="Exception">Optional exception if it was errored.</param>
public record ObserverInvocationResult(EventSequenceNumber LastSuccessfullyObservedEvent, Exception? Exception = null)
{
    /// <summary>
    /// Gets whether or not the result was successful.
    /// </summary>
    public bool IsSuccess => Exception is null;

    /// <summary>
    /// Gets a successful result.
    /// </summary>
    /// <param name="lastSuccessfullyObservedEvent">The sequence number of the last successful observed event.</param>
    /// <returns>A new <see cref="ObserverInvocationResult"/> instance.</returns>
    public static ObserverInvocationResult Success(EventSequenceNumber lastSuccessfullyObservedEvent) => new(lastSuccessfullyObservedEvent);

    /// <summary>
    /// Gets a successful result.
    /// </summary>
    /// <param name="lastSuccessfullyObservedEvent">The sequence number of the last successful observed event.</param>
    /// <param name="exception">The <see cref="Exception"/> that occurred.</param>
    /// <returns>A new <see cref="ObserverInvocationResult"/> instance.</returns>
    public static ObserverInvocationResult Failed(EventSequenceNumber lastSuccessfullyObservedEvent, Exception exception) => new(lastSuccessfullyObservedEvent, exception);
}
