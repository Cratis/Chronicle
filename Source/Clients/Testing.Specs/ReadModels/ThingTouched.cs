// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test event touching a thing — an event the read model only observes via <c>[FromAll]</c>, never an
/// explicit <c>[FromEvent]</c> subscription.
/// </summary>
/// <param name="Note">A note about the touch.</param>
[EventType]
public record ThingTouched(string Note);
