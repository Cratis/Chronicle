// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Engines.Keys;

namespace Aksio.Cratis.Kernel.Engines.Observation.Reducers;

/// <summary>
/// Represents the context for a reducer operation.
/// </summary>
/// <param name="Events">Collection of <see cref="AppendedEvent"/> to reduce from.</param>
/// <param name="Key"><see cref="Key"/> the events are for.</param>
/// <param name="IsReplaying">Whether or not the events are part of a replay.</param>
public record ReducerContext(IEnumerable<AppendedEvent> Events, Key Key, bool IsReplaying);
