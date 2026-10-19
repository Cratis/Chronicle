// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// The exception that is thrown when an observer has no handled event types because its handlers take archived generation types.
/// </summary>
/// <param name="artifactType">The observer artifact type.</param>
/// <param name="generationTypes">The archived generation types used by its handlers.</param>
public class ObserverHasOnlyGenerationHandlers(Type artifactType, IEnumerable<Type> generationTypes)
    : Exception($"Observer '{artifactType.FullName}' has no handled event types. Its handlers take generation-only types: {string.Join(", ", generationTypes.Select(type => $"'{type.FullName}'"))}. Handle an [EventType] type and pin the desired generation on the client instead of handling [EventTypeGenerationFor] types.")
{
    /// <summary>
    /// Refuses an empty typed observer with generation-only handler parameters.
    /// </summary>
    /// <param name="artifactType">The observer artifact type.</param>
    /// <param name="eventTypes">The resolved handled event types.</param>
    /// <exception cref="ObserverHasOnlyGenerationHandlers">The artifact only handles archived generation types.</exception>
    internal static void ThrowIfEmpty(Type artifactType, IEnumerable<EventType> eventTypes)
    {
        if (eventTypes.Any())
        {
            return;
        }

        var generationTypes = artifactType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(method => !method.IsSpecialName)
            .Select(method => method.GetParameters().FirstOrDefault()?.ParameterType)
            .Where(type => type is not null && Attribute.IsDefined(type, typeof(EventTypeGenerationForAttribute)))
            .Select(type => type!)
            .Distinct()
            .ToArray();
        if (generationTypes.Length > 0)
        {
            throw new ObserverHasOnlyGenerationHandlers(artifactType, generationTypes);
        }
    }
}
