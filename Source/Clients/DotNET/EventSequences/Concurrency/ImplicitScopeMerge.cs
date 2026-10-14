// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency;

/// <summary>
/// Applies source-wide implicit guards after the normal scope resolver has finished.
/// </summary>
internal static class ImplicitScopeMerge
{
    /// <summary>
    /// Merges implicit boundaries with the resolved scopes, preserving explicit and independent routing guards.
    /// </summary>
    /// <param name="resolved">Today's resolved scopes.</param>
    /// <param name="provenance">The commit's contributions.</param>
    /// <returns>The scopes to validate.</returns>
    /// <exception cref="ReservedConcurrencyScopeLabelCollision">A routing guard needs an occupied reserved label.</exception>
    internal static Dictionary<EventSourceId, ConcurrencyScope> Apply(Dictionary<EventSourceId, ConcurrencyScope> resolved, ScopeProvenance provenance)
    {
        foreach (var (label, implicitBoundary) in provenance.ImplicitScopes)
        {
            if (provenance.Origins.GetValueOrDefault(label).HasFlag(ScopeOrigins.Explicit))
            {
                ScopeGuardDiagnostics.DisplacedByExplicit();
                continue;
            }

            var boundary = implicitBoundary;
            if (resolved.TryGetValue(label, out var scope))
            {
                if (provenance.DecisionLabels.Contains(label))
                {
                    boundary = Earliest(boundary, scope.SequenceNumber);
                }
                else if (scope.EventSourceId is null && ChecksSomething(scope))
                {
                    EventSourceId reserved = $"$keyguard:routing:{label.Value}";
                    if (resolved.ContainsKey(reserved) || provenance.ImplicitScopes.ContainsKey(reserved))
                    {
                        throw new ReservedConcurrencyScopeLabelCollision(reserved);
                    }
                    resolved.Add(reserved, scope);
                }
            }
            resolved[label] = new(boundary, label);
        }
        return resolved;
    }

    /// <summary>
    /// Finds the earliest of two global boundaries, treating BeforeFirst as the smallest.
    /// </summary>
    /// <param name="first">The first boundary.</param>
    /// <param name="second">The second boundary.</param>
    /// <returns>The earliest boundary.</returns>
    internal static EventSequenceNumber Earliest(EventSequenceNumber first, EventSequenceNumber second)
    {
        if (first.IsBeforeFirst || second.IsBeforeFirst) return EventSequenceNumber.BeforeFirst;
        return first.Value <= second.Value ? first : second;
    }

    static bool ChecksSomething(ConcurrencyScope scope) =>
        scope != ConcurrencyScope.NotSet && scope != ConcurrencyScope.None && (scope.SequenceNumber.IsActualValue || scope.SequenceNumber.IsBeforeFirst);
}
