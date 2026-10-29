// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle;

/// <summary>
/// Derives artifact identities without deciding whether a type is an artifact.
/// </summary>
internal static class ArtifactIdentity
{
    /// <summary>
    /// Derives an event identity, including a historical generation's referenced identifier.
    /// </summary>
    /// <param name="type">The CLR type.</param>
    /// <returns>The event identity.</returns>
    /// <exception cref="EventTypeGenerationReferencesNonEventType">The historical generation references an unmarked type.</exception>
    internal static EventType EventTypeFor(Type type)
    {
        if (type.GetCustomAttribute<EventTypeGenerationForAttribute>() is { } generationFor)
        {
            var referencedType = generationFor.EventTypeClrType;
            var referencedAttribute = referencedType.GetCustomAttribute<EventTypeAttribute>() ??
                throw new EventTypeGenerationReferencesNonEventType(type, referencedType);
            return new EventType(EventIdFor(referencedType, referencedAttribute), generationFor.Generation);
        }

        var attribute = type.GetCustomAttribute<EventTypeAttribute>();
        return new EventType(EventIdFor(type, attribute), attribute?.Generation ?? EventTypeGeneration.First);
    }

    /// <summary>
    /// Derives the legacy observer identifier.
    /// </summary>
    /// <param name="type">The observer type.</param>
    /// <param name="declaredId">The optional declared identifier.</param>
    /// <returns>The observer identifier.</returns>
    internal static string ObserverIdFor(Type type, string? declaredId) =>
        string.IsNullOrEmpty(declaredId) ? type.FullName ?? $"{type.Namespace}.{type.Name}" : declaredId;

    /// <summary>
    /// Derives a read model identifier.
    /// </summary>
    /// <param name="type">The read model type.</param>
    /// <returns>The read model identifier.</returns>
    internal static string ReadModelIdFor(Type type) => type.FullName ?? type.Name;

    /// <summary>
    /// Derives a model-owned projection identifier without the observer fallback.
    /// </summary>
    /// <param name="type">The read model type.</param>
    /// <returns>The declared projection identifier, or the full-name default.</returns>
    internal static ProjectionId ModelProjectionIdFor(Type type)
    {
        var id = type.GetCustomAttribute<ProjectionAttribute>()?.Id;
        return id is not null && id.Value.Length > 0 ? id : new(type.FullName!);
    }

    /// <summary>
    /// Derives an event source name, preserving explicit empty names and suffix rules.
    /// </summary>
    /// <param name="type">The event source type.</param>
    /// <returns>The event source name.</returns>
    internal static string EventSourceNameFor(Type type) =>
        type.GetCustomAttribute<EventSourceAttribute>()?.Name ??
        (type.Name.Length > "EventSource".Length && type.Name.EndsWith("EventSource", StringComparison.Ordinal)
            ? type.Name[..^"EventSource".Length]
            : type.Name);

    /// <summary>
    /// Derives a constraint name from its owning member.
    /// </summary>
    /// <param name="member">The owning type or property.</param>
    /// <param name="declaredName">The optional declared name.</param>
    /// <returns>The constraint name.</returns>
    internal static string ConstraintNameFor(MemberInfo member, string? declaredName) => declaredName ?? member.Name;

    static EventTypeId EventIdFor(Type type, EventTypeAttribute? attribute) =>
        attribute is null || attribute.Id.Value.Length == 0 ? type.Name : attribute.Id.Value;
}
