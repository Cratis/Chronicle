// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency;

/// <summary>
/// An immutable acquisition or routing contribution, retained until commit.
/// </summary>
internal sealed record ReadProtectionContribution
{
    /// <summary>
    /// Captures a contribution and snapshots its predicate.
    /// </summary>
    /// <param name="label">The commit label.</param>
    /// <param name="kind">The acquisition or routing kind.</param>
    /// <param name="scope">The captured condition.</param>
    /// <param name="target">The full target identity.</param>
    /// <param name="description">The read or routing description.</param>
    /// <param name="intent">Explicit validation intent.</param>
    internal ReadProtectionContribution(EventSourceId label, ReadProtectionContributionKind kind, ConcurrencyScope scope, ReadProtectionTarget target, string description, ReadProtectionContributionIntent intent = ReadProtectionContributionIntent.None)
    {
        // A derived source record and its base representation share one wire label.
        Label = new(label.Value);
        Kind = kind;
        Scope = ReadProtectionPredicate.Normalize(scope);
        Target = target;
        Description = description;
        Intent = intent;
    }

    /// <summary>
    /// Gets the commit label.
    /// </summary>
    internal EventSourceId Label { get; }

    /// <summary>
    /// Gets the acquisition or routing kind.
    /// </summary>
    internal ReadProtectionContributionKind Kind { get; }

    /// <summary>
    /// Gets the snapshotted condition.
    /// </summary>
    internal ConcurrencyScope Scope { get; }

    /// <summary>
    /// Gets the full target identity.
    /// </summary>
    internal ReadProtectionTarget Target { get; }

    /// <summary>
    /// Gets the read or routing description.
    /// </summary>
    internal string Description { get; }

    /// <summary>
    /// Gets the accumulated validation intent.
    /// </summary>
    internal ReadProtectionContributionIntent Intent { get; init; }
}
