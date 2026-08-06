// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

/// <summary>
/// Event constrained to appear at most once per cycle through <see cref="UniqueAttribute"/> on the event type
/// itself, where the cycle is ended by <see cref="LoanReturned"/> - the lifecycle shape the unique event type
/// constraint could not express before it had a removal event.
/// </summary>
/// <param name="Title">The title that was checked out.</param>
[EventType]
[Unique(ConstraintName)]
public record LoanCheckedOut(string Title)
{
    /// <summary>
    /// The name of the unique event type constraint.
    /// </summary>
    public const string ConstraintName = "LoanOpen";
}
