// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test event the summary value-maps for its <see cref="WorkMode"/>. It also carries a <see cref="Location"/>
/// that collides by name with the summary's location — because the summary subscribes to it for a real value
/// mapping (not an aggregate), the aggregate heuristic does not apply, so only a property-level
/// <c>[NoAutoMap]</c> stops the collision.
/// </summary>
/// <param name="WorkMode">The work mode the summary maps.</param>
/// <param name="Location">A location on the event — deliberately named to collide.</param>
[EventType]
public record WorkModeSet(string WorkMode, string Location);
