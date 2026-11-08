// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle;

/// <summary>
/// Resolves the final named tags of an event about to be appended. Every append path goes through this single step.
/// </summary>
/// <remarks>
/// Order: the tags the event already has (call site), then the built-in enrichers (ambient scope, audience property),
/// then the discovered <see cref="IEventTagEnricher"/>s; the result is merged without duplicates. When the event type is
/// marked with <see cref="AudienceAttribute"/> the result must carry an audience tag, otherwise <see cref="MissingAudience"/> is thrown.
/// </remarks>
/// <param name="enrichers">The discovered <see cref="IEventTagEnricher"/>s, in addition to the built-in ones.</param>
public sealed class EventTagResolver(IEnumerable<IEventTagEnricher> enrichers)
{
    static readonly ConcurrentDictionary<Type, bool> _requiresAudience = new();
    readonly IEventTagEnricher[] _enrichers =
    [
        new AmbientEventTagEnricher(),
        new AudiencePropertyEventTagEnricher(),

        // Discovery also finds the built-in enrichers (they are already listed above) and can yield null for a
        // discovered type the service provider cannot create; neither belongs in the pipeline.
        .. enrichers.Where(_ => _ is not null and not AmbientEventTagEnricher and not AudiencePropertyEventTagEnricher)
    ];

    /// <summary>
    /// Gets a resolver with only the built-in enrichers.
    /// </summary>
    public static EventTagResolver Default { get; } = new([]);

    /// <summary>
    /// Resolves the named tags for the event.
    /// </summary>
    /// <param name="context">The <see cref="EventTagContext"/> carrying the tags the event already has.</param>
    /// <returns>The merged tags.</returns>
    /// <exception cref="MissingAudience">The event type requires an audience and none was provided.</exception>
    public IReadOnlyList<NamedTag> ResolveTags(EventTagContext context)
    {
        var added = _enrichers.SelectMany(enricher => enricher.GetTags(context) ?? []);
        var resolved = NamedTagConverters.Merge(context.ExistingTags, added);
        if (RequiresAudience(context.Event.GetType()) && !resolved.Any(_ => _.Name.Value == WellKnownTags.Audience))
        {
            throw new MissingAudience(context.Event.GetType());
        }

        return resolved;
    }

    /// <summary>
    /// Gets the event type for a CLR type, or an unknown event type when it is not registered, so that tag resolution never
    /// replaces the error an unregistered event type produces further down the append path.
    /// </summary>
    /// <param name="eventTypes">The <see cref="IEventTypes"/> to look in.</param>
    /// <param name="clrType">The CLR type of the event.</param>
    /// <returns>The <see cref="EventType"/>.</returns>
    internal static EventType EventTypeFor(IEventTypes eventTypes, Type clrType) =>
        eventTypes.HasFor(clrType) ? eventTypes.GetEventTypeFor(clrType) : new EventType(EventTypeId.Unknown, EventTypeGeneration.First);

    /// <summary>
    /// Determines whether the event type requires an audience.
    /// </summary>
    /// <param name="eventType">The CLR type of the event.</param>
    /// <returns>True when the type is marked with <see cref="AudienceAttribute"/>.</returns>
    internal static bool RequiresAudience(Type eventType) =>
        _requiresAudience.GetOrAdd(eventType, static type => Attribute.IsDefined(type, typeof(AudienceAttribute), true));
}
