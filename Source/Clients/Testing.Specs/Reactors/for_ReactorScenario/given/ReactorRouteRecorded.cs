// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

/// <summary>
/// Represents input used to verify reactor routing.
/// </summary>
/// <param name="Note">The input to distinguish deliveries.</param>
[EventType]
public record ReactorRouteRecorded(string Note);
