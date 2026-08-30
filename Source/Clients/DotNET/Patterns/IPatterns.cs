// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns;

/// <summary>
/// Defines a system for asking what usually happens, backed by the behavior patterns Chronicle mined from the
/// event history of an event store.
/// </summary>
public interface IPatterns
{
    /// <summary>
    /// Get the patterns that apply to a context, most specific and most confident first.
    /// </summary>
    /// <param name="groupingKey">The <see cref="PatternGroupingKey">scope</see> to ask within - typically a user.</param>
    /// <param name="context">The <see cref="FacetSet">context</see> to match, which may constrain any subset of the facets.</param>
    /// <param name="minimumConfidence">The lowest <see cref="PatternConfidence"/> an answer may hold. Defaults to the server's configured threshold.</param>
    /// <param name="maximumResults">The largest number of answers to return. Defaults to the server's default.</param>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns>The matching <see cref="BehaviorPattern">patterns</see>, empty when nothing clears the bar.</returns>
    /// <remarks>
    /// An empty result is an answer, not a failure: it says this scope has no established behavior for this
    /// context. Nothing is invented to fill the gap, so a caller can treat "no patterns" as "do not claim to know".
    /// </remarks>
    Task<IEnumerable<BehaviorPattern>> GetPatterns(
        PatternGroupingKey groupingKey,
        FacetSet context,
        PatternConfidence? minimumConfidence = default,
        int maximumResults = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get what usually happens in a context, most likely first, at most one answer per action.
    /// </summary>
    /// <param name="groupingKey">The <see cref="PatternGroupingKey">scope</see> to ask within - typically a user.</param>
    /// <param name="context">The <see cref="FacetSet">context</see> describing the situation, which may constrain any subset of the facets.</param>
    /// <param name="minimumConfidence">The lowest <see cref="PatternConfidence"/> an answer may hold. Defaults to the server's configured threshold.</param>
    /// <param name="maximumResults">The largest number of answers to return. Defaults to the server's default.</param>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="BehaviorPattern">patterns</see> naming what usually happens, empty when nothing clears the bar.</returns>
    /// <remarks>
    /// <para>
    /// Where <see cref="GetPatterns"/> returns patterns that <em>describe</em> a situation, this returns the ones
    /// that say what is <em>done</em> in it. Read the action off a result with
    /// <c>pattern.Facets.ValueOf(FacetName.CommandType)</c>, and its likelihood from <c>Confidence</c> - the chance
    /// of that action given the context it was established in.
    /// </para>
    /// <para>
    /// An empty result is an answer, not a failure: this scope has no established behavior for this context.
    /// </para>
    /// </remarks>
    Task<IEnumerable<BehaviorPattern>> GetUsualActions(
        PatternGroupingKey groupingKey,
        FacetSet context,
        PatternConfidence? minimumConfidence = default,
        int maximumResults = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get what a scope usually does at a moment, most likely first, at most one answer per action.
    /// </summary>
    /// <param name="groupingKey">The <see cref="PatternGroupingKey">scope</see> to ask about - typically a user.</param>
    /// <param name="moment">The moment to ask about. Defaults to now.</param>
    /// <param name="alsoConstraining">Optional further <see cref="FacetSet">facets</see> to narrow the question with.</param>
    /// <param name="minimumConfidence">The lowest <see cref="PatternConfidence"/> an answer may hold. Defaults to the server's configured threshold.</param>
    /// <param name="maximumResults">The largest number of answers to return. Defaults to the server's default.</param>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns>The matching <see cref="BehaviorPattern">patterns</see>, empty when nothing clears the bar.</returns>
    /// <remarks>
    /// The question an application actually has is "what does this person usually do right now", and this is that
    /// question. The day and the part of the day are read off the moment using the same rule the engine bucketed
    /// events with when it mined them, so the answer is about the slot the behavior was actually learned in.
    /// <para>
    /// Answers name what is done, through <see cref="GetUsualActions"/>. Pass <paramref name="alsoConstraining"/>
    /// to narrow the moment with more of the situation - the kind of thing being worked on, what caused the work.
    /// </para>
    /// <para>
    /// An empty result is an answer, not a failure: this scope has no established behavior for this moment.
    /// </para>
    /// </remarks>
    Task<IEnumerable<BehaviorPattern>> GetPatternsAt(
        PatternGroupingKey groupingKey,
        DateTimeOffset? moment = default,
        FacetSet? alsoConstraining = default,
        PatternConfidence? minimumConfidence = default,
        int maximumResults = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get every pattern established for a scope.
    /// </summary>
    /// <param name="groupingKey">The <see cref="PatternGroupingKey">scope</see> to get for.</param>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns>Every <see cref="BehaviorPattern"/> held for the scope.</returns>
    Task<IEnumerable<BehaviorPattern>> GetPatternsForScope(
        PatternGroupingKey groupingKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the scopes that have established patterns.
    /// </summary>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="PatternGroupingKey">scopes</see> holding patterns.</returns>
    Task<IEnumerable<PatternGroupingKey>> GetScopes(CancellationToken cancellationToken = default);
}
