// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// The exception that is thrown when a runtime reactor has no event types to observe.
/// </summary>
/// <param name="id">The reactor identifier.</param>
public class NoEventTypesForReactor(ReactorId id) : Exception($"Reactor '{id}' must subscribe to at least one event type.");
