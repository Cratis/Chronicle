// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections.Definitions;

/// <summary>
/// Represents the definition from for a set of events.
/// </summary>
/// <param name="EventTypes">Collection of <see cref="EventType"/> for the definition.</param>
/// <param name="From">The from definition associated.</param>
/// <remarks>
/// This is typically representing event types that are deriving from a common base type.
/// </remarks>
public record FromAnyDefinition(IEnumerable<EventType> EventTypes, FromDefinition From);
