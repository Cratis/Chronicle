// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Selects one index's pending range, including the coverage start of a closing declaration.
/// </summary>
/// <param name="Definition">The declaration at fold start.</param>
/// <param name="From">The first event to include for this index.</param>
public record ConstraintIndexReplayScope(IConstraintDefinition Definition, EventSequenceNumber From);
