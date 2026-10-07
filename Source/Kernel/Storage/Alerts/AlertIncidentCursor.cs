// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Alerts;

/// <summary>
/// Represents an exclusive keyset continuation.
/// </summary>
/// <param name="RaisedSequenceNumber">Last returned raise position.</param>
/// <param name="IncidentId">Last returned identity, ordered by its canonical string.</param>
public record AlertIncidentCursor(
    EventSequenceNumber RaisedSequenceNumber,
    IncidentId IncidentId);
