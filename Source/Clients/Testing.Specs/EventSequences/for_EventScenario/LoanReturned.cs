// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

/// <summary>
/// Releases the <see cref="LoanCheckedOut.ConstraintName"/> constraint through <see cref="RemoveConstraintAttribute"/>,
/// so the next checkout for the same borrower starts a new cycle.
/// </summary>
[EventType]
[RemoveConstraint(LoanCheckedOut.ConstraintName)]
public record LoanReturned;
