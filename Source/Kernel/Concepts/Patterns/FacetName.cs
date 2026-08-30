// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Patterns;

/// <summary>
/// Represents the name of a facet - one contextual dimension a <see cref="BehaviorPattern"/> can be expressed in.
/// </summary>
/// <param name="Value">The actual value.</param>
public record FacetName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Represents an unspecified <see cref="FacetName"/>.
    /// </summary>
    public static readonly FacetName Unspecified = new(string.Empty);

    /// <summary>
    /// The type of command - or, when nothing above the event named itself, the event type - that produced the event.
    /// </summary>
    public static readonly FacetName CommandType = new(nameof(CommandType));

    /// <summary>
    /// What kind of initiator caused the event - a user, an agent or the system itself.
    /// </summary>
    public static readonly FacetName InitiatorType = new(nameof(InitiatorType));

    /// <summary>
    /// The identifier of the initiator that caused the event.
    /// </summary>
    public static readonly FacetName InitiatorId = new(nameof(InitiatorId));

    /// <summary>
    /// The identity the initiator acted on behalf of, when it acted for someone else.
    /// </summary>
    public static readonly FacetName OnBehalfOf = new(nameof(OnBehalfOf));

    /// <summary>
    /// The command one level up the causation chain from the one that produced the event.
    /// </summary>
    public static readonly FacetName CausedByCommand = new(nameof(CausedByCommand));

    /// <summary>
    /// The correlation the event belongs to, identifying the flow it took part in.
    /// </summary>
    public static readonly FacetName CorrelationRootId = new(nameof(CorrelationRootId));

    /// <summary>
    /// The type of the event source the event was appended to.
    /// </summary>
    public static readonly FacetName AggregateType = new(nameof(AggregateType));

    /// <summary>
    /// The year the event occurred in.
    /// </summary>
    public static readonly FacetName Year = new(nameof(Year));

    /// <summary>
    /// The month the event occurred in.
    /// </summary>
    public static readonly FacetName Month = new(nameof(Month));

    /// <summary>
    /// The day of week the event occurred on.
    /// </summary>
    public static readonly FacetName Day = new(nameof(Day));

    /// <summary>
    /// The <see cref="Patterns.TimeBucket"/> the event occurred in.
    /// </summary>
    public static readonly FacetName TimeBucket = new(nameof(TimeBucket));

    /// <summary>
    /// Gets a value indicating whether the facet names the action that was taken rather than the context it was
    /// taken in.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The distinction is what separates the question from its answer. A caller describing a situation can name
    /// every context facet, because those are things they know - the day, the time of day, the kind of thing being
    /// worked on. They cannot name the action, because the action is what they are asking about.
    /// </para>
    /// <para>
    /// This is not a query-side convenience: confidence is already defined against it, as the frequency of an
    /// itemset over the frequency of the same itemset with its action facets removed - the chance of the action
    /// given the context. Naming another facet here therefore changes what confidence means for every pattern that
    /// constrains it, so it is a modelling decision rather than a filter.
    /// </para>
    /// <para>
    /// <see cref="CausedByCommand"/> is deliberately left as context. It reads as an answer to "what usually
    /// follows this" and as context to "what does this person do on a Monday", and only one of those can be true
    /// of a single definition.
    /// </para>
    /// </remarks>
    public bool IsAction => this == CommandType;

    /// <summary>
    /// Implicitly convert from a string to <see cref="FacetName"/>.
    /// </summary>
    /// <param name="name">String to convert from.</param>
    public static implicit operator FacetName(string name) => new(name);
}
