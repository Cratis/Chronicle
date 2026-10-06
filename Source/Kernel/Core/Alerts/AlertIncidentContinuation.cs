// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Alerts;

/// <summary>
/// Represents a paired keyset continuation.
/// </summary>
/// <param name="RaisedSequenceNumber">Exclusive raise position.</param>
/// <param name="IncidentId">Exclusive canonical identity.</param>
public record AlertIncidentContinuation(
    EventSequenceNumber RaisedSequenceNumber,
    IncidentId IncidentId);
