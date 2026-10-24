// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Generations.Bindings;

/// <summary>The store has no established generation incarnation.</summary>
/// <param name="store">The event store.</param>
public sealed class GenerationFloorNotEstablished(EventStoreName store) : InvalidOperationException($"The generation floor for '{store}' is not established.");
