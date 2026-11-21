// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Integration.SqlClustering;

/// <summary>
/// Describes everything an unsuccessful <see cref="AppendResult"/> reports, so a failure with no errors
/// (for example a concurrency or constraint violation) is still visible in the load log.
/// </summary>
public static class AppendFailureDescription
{
    /// <summary>
    /// Describes the failure information of an append result.
    /// </summary>
    /// <param name="result">The unsuccessful result.</param>
    /// <returns>A single line with errors, violations, conflicts and retry information.</returns>
    public static string Describe(AppendResult result) =>
        $"Errors=[{string.Join(';', result.Errors)}]; " +
        $"ConstraintViolations=[{string.Join(';', result.ConstraintViolations.Select(DescribeViolation))}]; " +
        $"ConcurrencyViolation={result.ConcurrencyViolation?.ToString() ?? "none"}; " +
        $"IdempotencyConflicts=[{string.Join(';', result.IdempotencyConflicts)}]; " +
        $"Outcome={result.Outcome}; ConcurrencyCheckPerformed={result.ConcurrencyCheckPerformed}; " +
        $"IsRetryable={result.IsRetryable}; RetryAfter={result.RetryAfter?.ToString() ?? "none"}; " +
        $"CorrelationId={result.CorrelationId}";

    static string DescribeViolation(ConstraintViolation violation) =>
        $"{violation} Details={{{string.Join(',', violation.Details.Select(detail => $"{detail.Key}={detail.Value}"))}}}";
}
