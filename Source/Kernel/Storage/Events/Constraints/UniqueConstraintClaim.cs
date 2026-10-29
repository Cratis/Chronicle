// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.Events.Constraints;

/// <summary>
/// Represents a value an event source wants to claim for a unique constraint.
/// </summary>
/// <param name="EventSourceId">The <see cref="EventSourceId"/> claiming the value.</param>
/// <param name="Value">The hashed <see cref="UniqueConstraintValue"/> being claimed.</param>
public record UniqueConstraintClaim(EventSourceId EventSourceId, UniqueConstraintValue Value);
