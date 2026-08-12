// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test event that carries both parent-level state (<see cref="Depot"/>) and child-level state
/// (<see cref="Worker"/> and <see cref="Hours"/>) — a single event type that a projection maps onto the
/// parent AND uses to feed a child collection.
/// </summary>
/// <param name="Depot">The depot name, mapped onto the parent.</param>
/// <param name="Worker">The worker, used as the child key.</param>
/// <param name="Hours">The hours logged for the worker.</param>
[EventType]
public record ShiftLogged(string Depot, string Worker, decimal Hours);
