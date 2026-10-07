// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Alerts;

/// <summary>
/// Represents a bounded page of open incidents.
/// </summary>
/// <param name="Items">Open rows in keyset order.</param>
/// <param name="Next">Continuation when another row exists.</param>
public record AlertIncidentStoragePage(
    IEnumerable<AlertIncident> Items,
    AlertIncidentCursor? Next);
