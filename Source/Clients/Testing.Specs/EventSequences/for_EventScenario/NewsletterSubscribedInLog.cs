// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

/// <summary>
/// Event constrained to appear at most once per event source, but only in the event log.
/// </summary>
/// <param name="Newsletter">The newsletter subscribed to.</param>
[EventType]
[Unique(ConstraintName, EventSequences = [EventSequenceId.LogId])]
public record NewsletterSubscribedInLog(string Newsletter)
{
    /// <summary>
    /// The name of the unique event type constraint.
    /// </summary>
    public const string ConstraintName = "NewsletterSubscribedInLogOnce";
}
