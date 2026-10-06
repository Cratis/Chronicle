// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Events;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Alerts;

internal static partial class AlertIncidentsReactorLogging
{
    [LoggerMessage(LogLevel.Warning, "Ignoring orphan escalation for incident {IncidentId} at sequence {SequenceNumber}")]
    internal static partial void OrphanEscalation(this ILogger<AlertIncidentsReactor> logger, IncidentId incidentId, EventSequenceNumber sequenceNumber);
}
