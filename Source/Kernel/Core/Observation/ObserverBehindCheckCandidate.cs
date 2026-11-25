// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents an observer whose cursor is compared with the tail of what it observes.
/// </summary>
/// <param name="ObserverKey">The <see cref="ObserverKey"/> of the observer.</param>
/// <param name="EventTypes">The <see cref="EventTypeSet"/> the observer observes.</param>
/// <param name="AllEventTypes">Whether the observer observes every event, whatever its type; <paramref name="EventTypes"/> is ignored when it does.</param>
public record ObserverBehindCheckCandidate(ObserverKey ObserverKey, EventTypeSet EventTypes, bool AllEventTypes);
