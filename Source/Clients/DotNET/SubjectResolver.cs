// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reflection;

namespace Cratis.Chronicle;

/// <summary>
/// Resolves a <see cref="Subject"/> from an event instance by looking for a property or
/// constructor parameter decorated with <see cref="SubjectAttribute"/>.
/// </summary>
public static class SubjectResolver
{
    static readonly ConcurrentDictionary<Type, PropertyInfo?> _cache = new();

    /// <summary>
    /// Attempt to derive a <see cref="Subject"/> from <paramref name="event"/> by reading the value
    /// of the first property or constructor parameter decorated with <see cref="SubjectAttribute"/>.
    /// </summary>
    /// <param name="event">The event instance to inspect.</param>
    /// <returns>
    /// The resolved <see cref="Subject"/> when a marked property or constructor parameter is found
    /// and its value is non-null; otherwise <see langword="null"/>.
    /// </returns>
    public static Subject? ResolveFrom(object @event)
    {
        var property = _cache.GetOrAdd(@event.GetType(), FindSubjectProperty);

        if (property is null)
        {
            return null;
        }

        var value = property.GetValue(@event);
        return value switch
        {
            null => null,
            Subject s => s,
            string str => str,
            Guid g => g,
            _ => value.ToString() is { } str and not "" ? new Subject(str) : null
        };
    }

    static PropertyInfo? FindSubjectProperty(Type t)
    {
        var properties = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // First: check property-level attribute ([property: Subject] explicit syntax)
        var prop = properties.FirstOrDefault(p => p.IsDefined(typeof(SubjectAttribute), inherit: false));
        if (prop is not null)
        {
            return prop;
        }

        // Second: check primary constructor parameters (record shorthand — [Subject] without [property:])
        var primaryCtor = t.GetConstructors().MaxBy(c => c.GetParameters().Length);
        if (primaryCtor is null)
        {
            return null;
        }

        var subjectParam = primaryCtor.GetParameters()
            .FirstOrDefault(p => p.IsDefined(typeof(SubjectAttribute), inherit: false));
        if (subjectParam is null)
        {
            return null;
        }

        return properties.FirstOrDefault(p =>
            string.Equals(p.Name, subjectParam.Name, StringComparison.OrdinalIgnoreCase));
    }
}
