// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

/// <summary>
/// Represents an event produced to verify its destination.
/// </summary>
/// <param name="Note">The input copied into the produced event.</param>
[EventType]
public record ProducedRouteRecorded(string Note);
