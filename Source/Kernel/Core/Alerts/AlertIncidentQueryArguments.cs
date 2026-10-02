// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Alerts;

namespace Cratis.Chronicle.Alerts;

/// <summary>
/// Validates required scope and paired continuation arguments.
/// </summary>
internal static class AlertIncidentQueryArguments
{
    /// <summary>
    /// Validates the affected store without broadening an invalid scope.
    /// </summary>
    /// <param name="eventStore">The required affected store.</param>
    /// <param name="namespace">Optional affected namespace.</param>
    /// <returns>The validated scope.</returns>
    /// <exception cref="InvalidAlertIncidentQuery">The store is not set.</exception>
    internal static AlertIncidentScope Scope(EventStoreName eventStore, EventStoreNamespaceName? @namespace)
    {
        if (eventStore == EventStoreName.NotSet || string.IsNullOrWhiteSpace(eventStore.Value))
        {
            throw new InvalidAlertIncidentQuery("An affected event store is required.");
        }

        return new(eventStore, @namespace);
    }

    /// <summary>
    /// Validates that a continuation is paired and contains an actual supported position.
    /// </summary>
    /// <param name="sequenceNumber">Exclusive raise position.</param>
    /// <param name="incidentId">Exclusive identity.</param>
    /// <returns>The paired continuation, or no continuation.</returns>
    /// <exception cref="InvalidAlertIncidentQuery">The continuation is incomplete or invalid.</exception>
    internal static AlertIncidentCursor? Cursor(EventSequenceNumber? sequenceNumber, IncidentId? incidentId)
    {
        if ((sequenceNumber is null) != (incidentId is null))
        {
            throw new InvalidAlertIncidentQuery("Both continuation fields must be supplied together.");
        }
        if (sequenceNumber is not null && (!sequenceNumber.IsActualValue || sequenceNumber.Value > long.MaxValue))
        {
            throw new InvalidAlertIncidentQuery("The continuation must contain an actual signed 64-bit sequence number.");
        }

        return sequenceNumber is null ? null : new(sequenceNumber, incidentId!);
    }
}
