// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Represents a root join definition of a projection whose applied event markers a sink keeps per document.
/// </summary>
/// <param name="DefId">The definition id, see <see cref="AppliedEventMarkers.DefinitionId"/>.</param>
/// <param name="EventTypeId">The id of the event type that joins.</param>
/// <param name="OnPath">The property path the join is on.</param>
public record AppliedEventMarkerJoin(string DefId, string EventTypeId, string OnPath);
