// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Patterns;

/// <summary>
/// Represents a recurring combination of facets that survived the support and confidence thresholds.
/// </summary>
/// <param name="GroupingKey">The scope the pattern belongs to.</param>
/// <param name="Facets">The <see cref="FacetSet"/> the pattern is expressed in.</param>
/// <param name="Occurrences">How many times the pattern has been observed.</param>
/// <param name="Confidence">How often the pattern holds when its context is present.</param>
/// <param name="Support">The share of all observed events the pattern was seen in.</param>
/// <param name="Weight">The recency-weighted strength of the pattern.</param>
/// <param name="FirstSeen">When the pattern was first observed.</param>
/// <param name="LastSeen">When the pattern was last observed.</param>
/// <remarks>
/// Only surviving patterns are persisted, so storage scales with distinct recurring behavior rather than with event
/// volume - a store that appends millions of events but sees a few hundred recurring behaviors holds a few hundred
/// rows here.
/// </remarks>
public record BehaviorPattern(
    PatternGroupingKey GroupingKey,
    FacetSet Facets,
    PatternOccurrences Occurrences,
    PatternConfidence Confidence,
    PatternSupport Support,
    PatternWeight Weight,
    DateTimeOffset FirstSeen,
    DateTimeOffset LastSeen)
{
    /// <summary>
    /// Gets how specific the pattern is - the number of facets it constrains.
    /// </summary>
    public int Specificity => Facets.Specificity;

    /// <summary>
    /// Gets the action the pattern names, or <see cref="FacetValue.Unspecified"/> when it is pure context.
    /// </summary>
    public FacetValue Action => Facets.Action;

    /// <summary>
    /// Gets how much of an asked context the pattern's own context uses - the number of context facets it constrains.
    /// </summary>
    /// <remarks>
    /// The specificity of an <em>answer</em>, as opposed to <see cref="Specificity"/>. Two patterns naming the same
    /// action differ by how much of the question they were conditioned on, and the action facet they share tells
    /// them apart in neither direction.
    /// </remarks>
    public int ContextSpecificity => Facets.Specificity - (Facets.ConstrainsAction ? 1 : 0);

    /// <summary>
    /// Check whether the pattern applies to a context.
    /// </summary>
    /// <param name="context">The <see cref="FacetSet"/> describing the context, which may constrain more facets than the pattern does.</param>
    /// <returns>True when every facet the pattern constrains is present with the same value in the context, false when not.</returns>
    /// <remarks>
    /// This is the question "does this established pattern describe the situation I am in" - the one worth asking
    /// about an action that already happened. It can never return a pattern naming an action the caller did not
    /// name, because such a pattern is not a subset of what was asked. Use <see cref="AnswersFor"/> for the
    /// question that has an action as its answer.
    /// </remarks>
    public bool Matches(FacetSet context) => Facets.IsSubsetOf(context);

    /// <summary>
    /// Check whether the pattern answers what usually happens in a context.
    /// </summary>
    /// <param name="context">The <see cref="FacetSet"/> describing the situation being asked about.</param>
    /// <returns>True when the pattern names an action and was established in a context the asked one covers, false when not.</returns>
    /// <remarks>
    /// <para>
    /// The action facet is excluded from the comparison on purpose. A caller asking what somebody usually does on a
    /// Monday morning cannot name the command in the question - naming it is what they are asking for - so requiring
    /// the whole pattern to be a subset of the question excludes exactly the patterns that answer it.
    /// </para>
    /// <para>
    /// An answer must also use some of the question. A pattern established in no context at all - "half of what
    /// this person ever does is post ledger entries" - is a subset of every context there is, so without this it
    /// answers every question identically, including questions about moments the scope has never worked. That
    /// would make "nothing is established here" unsayable for anyone with a dominant action, which is the one
    /// answer this whole surface exists to be able to give honestly. When the caller names nothing, though, the
    /// general behavior is precisely what they asked for, and it is returned.
    /// </para>
    /// </remarks>
    public bool AnswersFor(FacetSet context)
    {
        if (!Facets.ConstrainsAction)
        {
            return false;
        }

        var establishedIn = Facets.WithoutActions();

        return establishedIn.IsSubsetOf(context) && (!establishedIn.IsEmpty || context.IsEmpty);
    }
}
