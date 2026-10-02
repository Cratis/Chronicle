// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Converts contract partition recovery outcomes to client outcomes.
/// </summary>
internal static class ReactorPartitionRetryOutcomeConverters
{
    /// <summary>
    /// Converts a contract outcome to its client counterpart.
    /// </summary>
    /// <param name="outcome">The contract outcome.</param>
    /// <returns>The client outcome.</returns>
    internal static ReactorPartitionRetryOutcome ToClient(this Contracts.Observation.PartitionRecoveryOutcome outcome) =>
        outcome switch
        {
            Contracts.Observation.PartitionRecoveryOutcome.Started => ReactorPartitionRetryOutcome.Started,
            Contracts.Observation.PartitionRecoveryOutcome.PartitionNotFound => ReactorPartitionRetryOutcome.PartitionNotFound,
            Contracts.Observation.PartitionRecoveryOutcome.ObserverQuarantined => ReactorPartitionRetryOutcome.ObserverQuarantined,
            Contracts.Observation.PartitionRecoveryOutcome.PartitionQuarantined => ReactorPartitionRetryOutcome.PartitionQuarantined,
            _ => ReactorPartitionRetryOutcome.Unknown
        };

    /// <summary>
    /// Converts a contract clear-quarantine response to its client counterpart.
    /// </summary>
    /// <param name="response">The contract response.</param>
    /// <returns>The client result.</returns>
    internal static ReactorPartitionQuarantineClearResult ToClient(this Contracts.Observation.ClearPartitionQuarantineResponse response) =>
        new(
            response.Outcome switch
            {
                Contracts.Observation.ClearPartitionQuarantineOutcome.Cleared => ReactorPartitionQuarantineClearOutcome.Cleared,
                Contracts.Observation.ClearPartitionQuarantineOutcome.NotFound => ReactorPartitionQuarantineClearOutcome.NotFound,
                Contracts.Observation.ClearPartitionQuarantineOutcome.NotQuarantined => ReactorPartitionQuarantineClearOutcome.NotQuarantined,
                _ => ReactorPartitionQuarantineClearOutcome.Unknown
            },
            response.RetryOutcome.ToClient());
}
