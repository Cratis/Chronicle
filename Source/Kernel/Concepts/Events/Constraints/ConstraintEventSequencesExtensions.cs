// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Concepts.Events.Constraints;

/// <summary>
/// Extension methods for the event sequences a constraint applies to.
/// </summary>
/// <remarks>
/// A constraint declares the event sequences it applies to as a collection of <see cref="EventSequenceId"/>. An empty
/// collection is the default and means every event sequence - the behavior every constraint had before it could be
/// narrowed, and what a definition persisted or sent by an older client carries.
/// </remarks>
public static class ConstraintEventSequencesExtensions
{
    /// <summary>
    /// Check whether a set of declared event sequences covers a specific event sequence.
    /// </summary>
    /// <param name="eventSequences">The declared event sequences. Empty means every event sequence.</param>
    /// <param name="eventSequenceId">The <see cref="EventSequenceId"/> to check.</param>
    /// <returns>True if the event sequence is covered, false if not.</returns>
    public static bool Covers(this IEnumerable<EventSequenceId> eventSequences, EventSequenceId eventSequenceId)
    {
        var declared = eventSequences as IReadOnlyCollection<EventSequenceId> ?? eventSequences.ToArray();
        return declared.Count == 0 || declared.Contains(eventSequenceId);
    }

    /// <summary>
    /// Check whether two sets of declared event sequences cover the same event sequences.
    /// </summary>
    /// <param name="eventSequences">The declared event sequences.</param>
    /// <param name="other">The other declared event sequences.</param>
    /// <returns>True if they cover the same event sequences, false if not.</returns>
    /// <remarks>
    /// The comparison is by content and ignores order and duplicates, since the declaration is a set.
    /// </remarks>
    public static bool CoversSameAs(this IEnumerable<EventSequenceId> eventSequences, IEnumerable<EventSequenceId> other) =>
        eventSequences.ToHashSet().SetEquals(other);

    /// <summary>
    /// Check whether a set of declared event sequences covers any event sequence that a previous declaration did not.
    /// </summary>
    /// <param name="eventSequences">The declared event sequences.</param>
    /// <param name="previous">The previously declared event sequences.</param>
    /// <returns>True if it covers at least one event sequence the previous declaration did not, false if not.</returns>
    public static bool CoversMoreThan(this IEnumerable<EventSequenceId> eventSequences, IEnumerable<EventSequenceId> previous)
    {
        var previousDeclared = previous.ToHashSet();
        if (previousDeclared.Count == 0)
        {
            return false;
        }

        var declared = eventSequences.ToHashSet();
        return declared.Count == 0 || declared.Except(previousDeclared).Any();
    }

    /// <summary>
    /// Add the declared event sequences to a <see cref="HashCode"/> without depending on their order.
    /// </summary>
    /// <param name="hashCode">The <see cref="HashCode"/> to add to.</param>
    /// <param name="eventSequences">The declared event sequences.</param>
    public static void AddEventSequences(this ref HashCode hashCode, IEnumerable<EventSequenceId> eventSequences)
    {
        var combined = 0;
        foreach (var eventSequenceId in eventSequences.Distinct())
        {
            combined ^= eventSequenceId.GetHashCode();
        }

        hashCode.Add(combined);
    }
}
