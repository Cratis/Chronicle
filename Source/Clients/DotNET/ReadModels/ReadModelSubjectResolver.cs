// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reflection;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Resolves a <see cref="Subject"/> from a read model instance by checking for a property or constructor
/// parameter decorated with <see cref="SubjectAttribute"/>, falling back to a property named <c>Id</c>.
/// </summary>
public static class ReadModelSubjectResolver
{
    static readonly ConcurrentDictionary<Type, PropertyInfo?> _cache = new();

    /// <summary>
    /// Attempt to derive a <see cref="Subject"/> from a read model instance.
    /// <para>Resolution order:</para>
    /// <list type="number">
    ///   <item><description>Property decorated with <see cref="SubjectAttribute"/>.</description></item>
    ///   <item><description>Constructor parameter decorated with <see cref="SubjectAttribute"/> (record shorthand).</description></item>
    ///   <item><description>Property named <c>Id</c> (case-insensitive).</description></item>
    /// </list>
    /// </summary>
    /// <param name="instance">The read model instance to inspect.</param>
    /// <returns>The resolved <see cref="Subject"/>, or <see langword="null"/> when no subject can be derived.</returns>
    public static Subject? ResolveFrom(object instance)
    {
        var property = _cache.GetOrAdd(instance.GetType(), FindSubjectProperty);

        if (property is null)
        {
            return null;
        }

        var value = property.GetValue(instance);
        return value switch
        {
            null => null,
            Subject s => s,
            string str when !string.IsNullOrEmpty(str) => str,
            Guid g when g != Guid.Empty => g,
            _ => value.ToString() is { } str and not "" ? new Subject(str) : null
        };
    }

    static PropertyInfo? FindSubjectProperty(Type t)
    {
        var properties = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // 1. Explicit [Subject] on property
        var prop = properties.FirstOrDefault(p => p.IsDefined(typeof(SubjectAttribute), inherit: false));
        if (prop is not null)
        {
            return prop;
        }

        // 2. [Subject] on constructor parameter (record shorthand, without [property:])
        var primaryCtor = t.GetConstructors().MaxBy(c => c.GetParameters().Length);
        var subjectParam = primaryCtor?.GetParameters()
            .FirstOrDefault(p => p.IsDefined(typeof(SubjectAttribute), inherit: false));

        if (subjectParam is not null)
        {
            return properties.FirstOrDefault(p =>
                string.Equals(p.Name, subjectParam.Name, StringComparison.OrdinalIgnoreCase));
        }

        // 3. Fallback: property named "Id"
        return properties.FirstOrDefault(p =>
            string.Equals(p.Name, "Id", StringComparison.OrdinalIgnoreCase));
    }
}
