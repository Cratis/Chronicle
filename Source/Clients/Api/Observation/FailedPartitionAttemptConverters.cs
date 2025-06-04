// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Observation;

/// <summary>
/// Converters between contracts and API models for <see cref="FailedPartitionAttempt"/>.
/// </summary>
internal static class FailedPartitionAttemptConverters
{
    /// <summary>
    /// Converts a <see cref="Contracts.Observation.FailedPartitionAttempt"/> to a <see cref="FailedPartitionAttempt"/>.
    /// </summary>
    /// <param name="failedPartitionAttempt">The failed partition attempt to convert.</param>
    /// <returns>The converted failed partition attempt.</returns>
    public static FailedPartitionAttempt ToApi(this Contracts.Observation.FailedPartitionAttempt failedPartitionAttempt) =>
        new(failedPartitionAttempt.Occurred, failedPartitionAttempt.SequenceNumber, failedPartitionAttempt.Messages, failedPartitionAttempt.StackTrace);

    /// <summary>
    /// Converts a collection of <see cref="Contracts.Observation.FailedPartitionAttempt"/> to a collection of <see cref="FailedPartitionAttempt"/>.
    /// </summary>
    /// <param name="failedPartitionAttempts">The failed partition attempts to convert.</param>
    /// <returns>The converted failed partition attempts.</returns>
    public static IEnumerable<FailedPartitionAttempt> ToApi(this IEnumerable<Contracts.Observation.FailedPartitionAttempt> failedPartitionAttempts) =>
        failedPartitionAttempts.Select(ToApi);
}
