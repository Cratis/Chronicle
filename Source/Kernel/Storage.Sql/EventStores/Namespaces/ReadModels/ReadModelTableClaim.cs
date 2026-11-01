// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Ownership evidence read from persisted definitions, occurrences and completion markers.
/// Readers must preserve the originating event store and namespace, including in shared placement.
/// </summary>
/// <param name="Owner">The complete attributed owner.</param>
/// <param name="Registered">Whether a persisted definition attributes a live table to this owner.</param>
/// <param name="Occurred">Whether a persisted occurrence attributes a backup to this owner.</param>
/// <param name="PromotedPhysicalName">The explicit physical owner in a matching completion marker.</param>
/// <param name="Displaced">Whether a completion marker explicitly lost physical ownership.</param>
internal sealed record ReadModelTableClaim(
    ReadModelTableOwner Owner,
    bool Registered = false,
    bool Occurred = false,
    string? PromotedPhysicalName = null,
    bool Displaced = false);
