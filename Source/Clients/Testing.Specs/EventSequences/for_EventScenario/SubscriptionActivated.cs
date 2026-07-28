// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

/// <summary>
/// Event constrained to appear at most once per event source through <see cref="UniqueAttribute"/> on the event
/// type itself - the unique event type constraint, which reads the appended events rather than a separate index.
/// </summary>
/// <param name="Plan">The plan the subscription was activated on.</param>
[EventType]
[Unique(ConstraintName)]
public record SubscriptionActivated(string Plan)
{
    /// <summary>
    /// The name of the unique event type constraint.
    /// </summary>
    public const string ConstraintName = "SubscriptionActivatedOnce";
}
