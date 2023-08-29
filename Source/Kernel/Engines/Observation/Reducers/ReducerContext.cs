// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Keys;

namespace Aksio.Cratis.Kernel.Engines.Observation.Reducers;

/// <summary>
/// Represents the context for a reducer operation.
/// </summary>
/// <param name="Events">Collection of <see cref="AppendedEvent"/> to reduce from.</param>
/// <param name="Key"><see cref="Key"/> the events are for.</param>
/// <param name="ObservationState"><see cref="EventObservationState"/> for the context. Each individual event context on each event will be exact what the events observation state is.</param>
public record ReducerContext(IEnumerable<AppendedEvent> Events, Key Key, EventObservationState ObservationState);
