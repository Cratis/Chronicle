// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Alerts;

/// <summary>
/// Defines the common storage boundary and canonical ordering.
/// </summary>
public static class AlertIncidentStorageRules
{
    /// <summary>
    /// Validates the signed sequence storage boundary for every provider.
    /// </summary>
    /// <param name="value">Sequence to validate.</param>
    /// <exception cref="AlertIncidentSequenceNumberOutOfRange">The value exceeds signed storage range.</exception>
    public static void Validate(EventSequenceNumber value)
    {
        if (value.Value > long.MaxValue)
        {
            throw new AlertIncidentSequenceNumberOutOfRange(value);
        }
    }

    /// <summary>
    /// Normalizes a page size to the supported range.
    /// </summary>
    /// <param name="limit">Requested limit.</param>
    /// <returns>A limit between one and 500.</returns>
    public static int Limit(int limit) => Math.Clamp(limit, 1, 500);

    /// <summary>
    /// Gets the canonical incident key used for ordinal ordering.
    /// </summary>
    /// <param name="id">Incident identity.</param>
    /// <returns>The lowercase 32-character key.</returns>
    public static string Key(IncidentId id) => id.Value.ToString("N");

    /// <summary>
    /// Confirms the outcome of an unmatched conditional write.
    /// </summary>
    /// <param name="current">Diagnostic reread of the row.</param>
    /// <param name="transition">Requested transition.</param>
    /// <returns>The confirmed no-op outcome.</returns>
    /// <exception cref="AlertIncidentWriteNotConfirmed">The outcome cannot be determined.</exception>
    public static AlertIncidentWriteOutcome Confirm(AlertIncident? current, AlertIncidentTransition transition)
    {
        if (current is not null && current.LastTransitionSequenceNumber >= transition.SequenceNumber)
        {
            return AlertIncidentWriteOutcome.AlreadyApplied;
        }
        if (transition.Kind == AlertIncidentTransitionKind.Escalated && (current?.IsOpen != true))
        {
            return AlertIncidentWriteOutcome.OrphanEscalation;
        }

        throw new AlertIncidentWriteNotConfirmed(transition.Id);
    }
}
