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
/// <param name="Failed">The constraints whose writes failed in this turn.</param>
public record ConstraintIndexReplayResult(bool IsCompleted, int Applied, int Settled, int SettledWithError, IEnumerable<ConstraintName> Failed);
