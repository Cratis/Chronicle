// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Settles append grain calls whose response can be lost after the write committed.
/// </summary>
internal static class AppendCompletion
{
    /// <summary>
    /// Reports lost confirmation of a dispatched single append without claiming rejection.
    /// </summary>
    /// <param name="append">The dispatched grain call.</param>
    /// <param name="correlationId">The correlation id used for reconciliation.</param>
    /// <returns>The append result or an outcome-unknown error.</returns>
    internal static Task<AppendResult> WithUnknownOutcome(this Task<AppendResult> append, CorrelationId correlationId) =>
        Complete(append, () => new AppendResult
        {
            CorrelationId = correlationId,
            Errors = [AppendError.AppendOutcomeUnknown]
        });

    /// <summary>
    /// Reports lost confirmation of a dispatched batch append without claiming rejection.
    /// </summary>
    /// <param name="append">The dispatched grain call.</param>
    /// <param name="correlationId">The correlation id used for reconciliation.</param>
    /// <returns>The batch result or an outcome-unknown error.</returns>
    internal static Task<AppendManyResult> WithUnknownOutcome(this Task<AppendManyResult> append, CorrelationId correlationId) =>
        Complete(append, () => new AppendManyResult
        {
            CorrelationId = correlationId,
            Errors = [AppendError.AppendOutcomeUnknown]
        });

    static async Task<TResult> Complete<TResult>(Task<TResult> append, Func<TResult> unknownOutcome)
    {
        try
        {
            return await append;
        }
        catch (Exception exception) when (exception is SiloUnavailableException or TimeoutException)
        {
            return unknownOutcome();
        }
    }
}
