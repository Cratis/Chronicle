// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Reducers;

/// <summary>
/// Represents the context for a reducer operation.
/// </summary>
/// <param name="Events">Collection of <see cref="AppendedEvent"/> to reduce from.</param>
/// <param name="Key"><see cref="Key"/> the events are for.</param>
public record ReducerContext(IEnumerable<AppendedEvent> Events, Key Key);
