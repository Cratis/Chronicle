// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Carries an <see cref="EventTypeSet"/> between grains as its event types. The set itself is shared with the clients and does not reference Orleans.
/// </summary>
/// <param name="EventTypes">The <see cref="EventType">event types</see> in the set.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Setup.Serialization.EventTypeSetSurrogate")]
public readonly record struct EventTypeSetSurrogate([property: Id(0)] IReadOnlyList<EventType> EventTypes);
