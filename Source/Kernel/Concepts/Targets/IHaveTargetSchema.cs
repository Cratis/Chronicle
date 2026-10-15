// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Concepts.Targets;

/// <summary>
/// Defines schema resolution for a projection or reducer target independently of how its state is stored.
/// </summary>
/// <remarks>
/// Read models resolve their latest registered schema. Event targets resolve the schema of their explicitly
/// registered event type and generation; resolving a target does not register or advance a generation.
/// This contract does not select a sink or authorize publication to an event sequence.
/// </remarks>
public interface IHaveTargetSchema
{
    /// <summary>
    /// Resolve the schema used to construct target instances.
    /// </summary>
    /// <returns>The registered target schema.</returns>
    JsonSchema GetTargetSchema();
}
