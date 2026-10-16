// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Identifies a kernel constraint rebuild by its stable job step identity.
/// </summary>
/// <param name="Value">The job step identifier.</param>
public record ConstraintIndexRebuildRunId(Guid Value) : ConceptAs<Guid>(Value);
