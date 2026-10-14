// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Reports bounded replay progress without admitting appends on unfinished indexes.
/// </summary>
/// <param name="IsCompleted">Whether all differences have been written or settled.</param>
/// <param name="Applied">The successful writes so far.</param>
/// <param name="Settled">The refused claims settled so far.</param>
/// <param name="SettledWithError">The unresolved closing events settled so far.</param>
/// <param name="Failed">The first storage or content failure per constraint in this turn, excluding abandoned attempts.</param>
/// <param name="Abandoned">The constraints with an unconfirmed one-second attempt, distinct from failures and budget expiry.</param>
public record ConstraintIndexReplayResult(bool IsCompleted, int Applied, int Settled, int SettledWithError, IReadOnlyDictionary<ConstraintName, ConstraintIndexReplayFailure> Failed, IEnumerable<ConstraintName> Abandoned);
