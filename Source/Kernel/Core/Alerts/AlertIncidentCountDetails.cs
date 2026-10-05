// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Alerts;

/// <summary>
/// Represents a scoped incident count bucket.
/// </summary>
/// <param name="Namespace">Affected namespace.</param>
/// <param name="Condition">Recorded condition.</param>
/// <param name="Severity">Recorded severity.</param>
/// <param name="Count">Complete matching open count.</param>
public record AlertIncidentCountDetails(
    EventStoreNamespaceName Namespace,
    AlertConditionKind Condition,
    AlertSeverity Severity,
    long Count);
