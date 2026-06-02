// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.SequenceQueries;

/// <summary>
/// Defines the event type identifiers used for sequence query workbench events.
/// </summary>
public static class SequenceQueryEventTypeIds
{
    /// <summary>
    /// Event type id for when a sequence query is created.
    /// </summary>
    public const string Created = "cratis.chronicle.workbench.sequence-query-created";

    /// <summary>
    /// Event type id for when a sequence query is renamed.
    /// </summary>
    public const string Renamed = "cratis.chronicle.workbench.sequence-query-renamed";

    /// <summary>
    /// Event type id for when a sequence query filter is updated.
    /// </summary>
    public const string FilterUpdated = "cratis.chronicle.workbench.sequence-query-filter-updated";
}
