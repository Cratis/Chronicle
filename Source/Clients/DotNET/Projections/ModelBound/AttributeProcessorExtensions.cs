// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Contracts.Projections;
using EventType = Cratis.Chronicle.Contracts.Events.EventType;

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Extension methods for processing projection attributes on members.
/// </summary>
static class AttributeProcessorExtensions
{
    /// <summary>
    /// Processes attributes of a specific type on a member and applies a mapping action for each.
    /// </summary>
    /// <typeparam name="TAttribute">The type of attribute to process.</typeparam>
    /// <param name="member">The member to process attributes from.</param>
    /// <param name="propertyName">The property name on the projection model.</param>
    /// <param name="targetFrom">The target From dictionary to add mappings to.</param>
    /// <param name="mappingAction">The action to invoke for each matching attribute.</param>
    internal static void ProcessAttributesByType<TAttribute>(
        this MemberInfo member,
        string propertyName,
        IDictionary<EventType, FromDefinition> targetFrom,
        Action<IDictionary<EventType, FromDefinition>, Type, string, string> mappingAction)
    {
        var attributes = member.GetCustomAttributes()
            .Where(attr => attr.GetType().IsGenericType &&
                          attr.GetType().GetGenericTypeDefinition() == typeof(TAttribute).GetGenericTypeDefinition())
            .ToList();

        foreach (var attr in attributes)
        {
            var eventType = attr.GetType().GetGenericArguments()[0];
            var eventPropertyNameProperty = attr.GetType().GetProperty("EventPropertyName");
            var eventPropertyName = eventPropertyNameProperty?.GetValue(attr) as string;

            mappingAction(targetFrom, eventType, propertyName, eventPropertyName ?? member.Name);
        }
    }

    /// <summary>
    /// Processes attributes of a specific type on a parameter and applies a mapping action for each.
    /// </summary>
    /// <typeparam name="TAttribute">The type of attribute to process.</typeparam>
    /// <param name="parameter">The parameter to process attributes from.</param>
    /// <param name="propertyName">The property name on the projection model.</param>
    /// <param name="targetFrom">The target From dictionary to add mappings to.</param>
    /// <param name="mappingAction">The action to invoke for each matching attribute.</param>
    internal static void ProcessParameterAttributesByType<TAttribute>(
        this ParameterInfo parameter,
        string propertyName,
        IDictionary<EventType, FromDefinition> targetFrom,
        Action<IDictionary<EventType, FromDefinition>, Type, string, string> mappingAction)
    {
        var attributes = parameter.GetCustomAttributes()
            .Where(attr => attr.GetType().IsGenericType &&
                          attr.GetType().GetGenericTypeDefinition() == typeof(TAttribute).GetGenericTypeDefinition())
            .ToList();

        foreach (var attr in attributes)
        {
            var eventType = attr.GetType().GetGenericArguments()[0];
            var eventPropertyNameProperty = attr.GetType().GetProperty("EventPropertyName");
            var eventPropertyName = eventPropertyNameProperty?.GetValue(attr) as string;

            mappingAction(targetFrom, eventType, propertyName, eventPropertyName ?? parameter.Name!);
        }
    }

    /// <summary>
    /// Gets all attributes of a generic type from a custom attribute provider.
    /// </summary>
    /// <typeparam name="TAttribute">The generic type definition to match.</typeparam>
    /// <param name="provider">The custom attribute provider to get attributes from.</param>
    /// <returns>An enumerable of tuples containing the attribute and its event type.</returns>
    internal static IEnumerable<(Attribute Attribute, Type EventType)> GetAttributesOfGenericType<TAttribute>(this ICustomAttributeProvider provider)
    {
        var attributes = provider.GetCustomAttributes(inherit: false)
            .Cast<Attribute>()
            .Where(attr => attr.GetType().IsGenericType &&
                          attr.GetType().GetGenericTypeDefinition() == typeof(TAttribute).GetGenericTypeDefinition());

        foreach (var attr in attributes)
        {
            var eventType = attr.GetType().GetGenericArguments()[0];
            yield return (attr, eventType);
        }
    }
}
