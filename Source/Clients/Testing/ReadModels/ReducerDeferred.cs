// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// The exception that is thrown when a batch reducer defers the event source a scenario reduces, so no read model is written.
/// </summary>
/// <param name="readModelType">The read model type whose reducer deferred.</param>
/// <param name="retryAfter">How long the reducer asked to wait before the events are delivered again.</param>
public class ReducerDeferred(Type readModelType, TimeSpan retryAfter)
    : Exception($"Reducer for read model type '{readModelType.FullName}' deferred the event source for {retryAfter}. Chronicle delivers the events again after that delay, and writes no read model until then.")
{
    /// <summary>
    /// Gets how long the reducer asked to wait before the events are delivered again.
    /// </summary>
    public TimeSpan RetryAfter { get; } = retryAfter;
}
