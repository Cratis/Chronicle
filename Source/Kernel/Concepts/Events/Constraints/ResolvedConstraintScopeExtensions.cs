// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.Constraints;

/// <summary>
/// Extension methods for <see cref="ResolvedConstraintScope"/>.
/// </summary>
public static class ResolvedConstraintScopeExtensions
{
    /// <summary>
    /// Render the resolved scope as the flattened scope key older storage providers were given.
    /// </summary>
    /// <param name="scope">The <see cref="ResolvedConstraintScope"/> to render, or <see langword="null"/> when unscoped.</param>
    /// <returns>The flattened key, empty when nothing is narrowed.</returns>
    /// <remarks>
    /// This exists for one purpose: handing a provider that only implements the older
    /// <c>IsAllowed(definition, eventSourceId, scopeKey)</c> member the exact key it used to receive, so that
    /// provider keeps behaving as it did. It emits the same <c>est:</c>, <c>estt:</c> and <c>esid:</c> parts, in the
    /// same fixed order, omitting the same dimensions, as <see cref="ConstraintScopeExtensions.BuildScopeKey"/>
    /// does for the same event - a resolved scope is exactly the set of dimensions that key would have included.
    /// <para>
    /// Deterministic in that direction only. Flattening is <em>not</em> injective: two genuinely different
    /// dimension tuples can render to the same text when a value contains the characters the key joins on, which is
    /// the aliasing weakness the typed scope exists to remove. Going through this bridge therefore keeps a legacy
    /// provider exactly as correct - and exactly as wrong - as it already was. It does not repair it.
    /// </para>
    /// <para>
    /// The key is write-only. Nothing may parse a scope back out of it, and no built-in provider, batch claim or
    /// kernel path may narrow by it: those all carry the typed <see cref="ResolvedConstraintScope"/> end to end.
    /// </para>
    /// </remarks>
    public static string ToLegacyScopeKey(this ResolvedConstraintScope? scope)
    {
        if (scope is null)
        {
            return string.Empty;
        }

        var parts = new List<string>();

        if (scope.EventSourceType is not null)
        {
            parts.Add($"est:{scope.EventSourceType.Value}");
        }

        if (scope.EventStreamType is not null)
        {
            parts.Add($"estt:{scope.EventStreamType.Value}");
        }

        if (scope.EventStreamId is not null)
        {
            parts.Add($"esid:{scope.EventStreamId.Value}");
        }

        return string.Join('|', parts);
    }
}
