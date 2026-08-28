// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Patterns;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Patterns;

/// <summary>
/// Represents an implementation of <see cref="IEventFeatureExtractor"/>.
/// </summary>
/// <param name="timeBucketResolver"><see cref="ITimeBucketResolver"/> for resolving the part of the day.</param>
/// <remarks>
/// Nothing here reads the event's content. Patterns are mined from the context an event was appended in - who, on
/// whose behalf, caused by what, against what kind of event source, when - which is the same vocabulary for every
/// event type in every event store, and is exactly what makes the mined patterns comparable across a store.
/// </remarks>
[Singleton]
public class EventFeatureExtractor(ITimeBucketResolver timeBucketResolver) : IEventFeatureExtractor
{
    /// <inheritdoc/>
    public EventFeatures Extract(AppendedEvent @event)
    {
        var context = @event.Context;
        var causedBy = context.CausedBy ?? Identity.NotSet;
        var rootIdentity = GetRootIdentity(causedBy);
        var (commandType, causedByCommand) = GetCommandTypes(context);

        return new EventFeatures(
            GetGroupingKey(rootIdentity),
            commandType,
            GetInitiatorType(causedBy),
            GetIdentitySubject(causedBy),
            GetIdentitySubject(causedBy.OnBehalfOf),
            causedByCommand,
            context.CorrelationId.ToString(),
            context.EventSourceType.IsDefaultOrUnspecified ? FacetValue.Unspecified : context.EventSourceType.Value,
            context.Occurred.Year,
            context.Occurred.Month,
            context.Occurred.DayOfWeek,
            timeBucketResolver.Resolve(context.Occurred),
            context.Occurred);
    }

    /// <summary>
    /// Gets the command types for an event - the one that caused it and the one a level above that.
    /// </summary>
    /// <param name="context">The <see cref="EventContext"/> to read the causation chain from.</param>
    /// <returns>The command type and the command that caused it.</returns>
    /// <remarks>
    /// The causation chain is ordered from the root outwards, so the last named link is what directly produced the
    /// event and the one before it is a level up. Links the client stack could not name - <see cref="CausationType.Root"/>
    /// and <see cref="CausationType.Unknown"/> - carry no behavior worth mining and are skipped. When nothing above
    /// the event named itself, the event type stands in: in an event-sourced store the fact that was recorded is
    /// itself the action.
    /// </remarks>
    static (FacetValue CommandType, FacetValue CausedByCommand) GetCommandTypes(EventContext context)
    {
        var named = context.Causation?
            .Where(causation => IsNamed(causation.Type))
            .Select(causation => causation.Type.Value)
            .ToArray() ?? [];

        FacetValue commandType = named.Length > 0 ? named[^1] : context.EventType.Id.Value;
        var causedByCommand = named.Length > 1 ? new FacetValue(named[^2]) : FacetValue.Unspecified;

        return (commandType, causedByCommand);
    }

    static bool IsNamed(CausationType type) =>
        !string.IsNullOrEmpty(type?.Value) && type != CausationType.Root && type != CausationType.Unknown;

    /// <summary>
    /// Gets the scope the behavior belongs to.
    /// </summary>
    /// <param name="rootIdentity">The identity at the root of the on-behalf-of chain.</param>
    /// <returns>The <see cref="PatternGroupingKey"/>.</returns>
    /// <remarks>
    /// An agent acting for a person contributes to that person's behavior, not to its own - otherwise the same
    /// habit would be mined once per agent that happened to carry it out, and none of the halves would clear the
    /// support threshold.
    /// </remarks>
    static PatternGroupingKey GetGroupingKey(Identity rootIdentity) =>
        IsAnonymous(rootIdentity) ? PatternGroupingKey.Unspecified : rootIdentity.Subject;

    static Identity GetRootIdentity(Identity identity)
    {
        var current = identity;
        while (current.OnBehalfOf is not null)
        {
            current = current.OnBehalfOf;
        }

        return current;
    }

    static FacetValue GetIdentitySubject(Identity? identity) =>
        identity is null || IsAnonymous(identity) ? FacetValue.Unspecified : identity.Subject;

    static bool IsAnonymous(Identity identity) =>
        identity == Identity.NotSet || identity == Identity.Unknown || string.IsNullOrEmpty(identity.Subject);

    /// <summary>
    /// Gets what kind of initiator caused the event.
    /// </summary>
    /// <param name="causedBy">The <see cref="Identity"/> that caused it.</param>
    /// <returns>The <see cref="InitiatorType"/>.</returns>
    /// <remarks>
    /// Acting on behalf of somebody is what separates an agent from a user: a person acts as themselves, whereas
    /// anything carrying a delegation chain is standing in for one.
    /// </remarks>
    static InitiatorType GetInitiatorType(Identity causedBy) => causedBy switch
    {
        _ when causedBy == Identity.System => InitiatorType.System,
        _ when IsAnonymous(causedBy) => InitiatorType.Unknown,
        { OnBehalfOf: not null } => InitiatorType.Agent,
        _ => InitiatorType.User
    };
}
