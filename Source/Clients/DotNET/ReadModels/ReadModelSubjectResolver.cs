// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reflection;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Resolves a <see cref="Subject"/> from a read model instance by checking for a property or constructor
/// parameter decorated with <see cref="SubjectAttribute"/> that has a value, falling back to a property named <c>Id</c>.
/// </summary>
public static class ReadModelSubjectResolver
{
    static readonly ConcurrentDictionary<Type, SubjectProperties> _cache = new();

    /// <summary>
    /// Attempt to derive a <see cref="Subject"/> from a read model instance.
    /// <para>Resolution order:</para>
    /// <list type="number">
    ///   <item><description>A property decorated with <see cref="SubjectAttribute"/> that has a value.</description></item>
    ///   <item><description>A constructor parameter decorated with <see cref="SubjectAttribute"/> that has a value (record shorthand).</description></item>
    ///   <item><description>Property named <c>Id</c> (case-insensitive).</description></item>
    /// </list>
    /// An attributed property that is null, empty, or <see cref="Subject.NotSet"/> does not stop resolution;
    /// the <c>Id</c> fallback is attempted next.
    /// </summary>
    /// <param name="instance">The read model instance to inspect, or <see langword="null"/> for a read model that does not exist (never created or removed).</param>
    /// <returns>The resolved <see cref="Subject"/>, or <see langword="null"/> when no subject can be derived (including when <paramref name="instance"/> is <see langword="null"/>).</returns>
    public static Subject? ResolveFrom(object? instance)
    {
        if (instance is null)
        {
            return null;
        }

        var properties = _cache.GetOrAdd(instance.GetType(), FindSubjectProperties);
        return ToSubject(properties.Explicit?.GetValue(instance)) ??
               ToSubject(properties.Id?.GetValue(instance));
    }

    /// <summary>
    /// Convert a read model property value to the <see cref="Subject"/> it stands for.
    /// </summary>
    /// <param name="value">The property value to convert.</param>
    /// <returns>The <see cref="Subject"/>, or <see langword="null"/> when the value stands for none.</returns>
    internal static Subject? ToSubject(object? value) =>
        value switch
        {
            null => null,
            Subject s when s.IsSet => s,
            string str when !string.IsNullOrEmpty(str) => str,
            Guid g when g != Guid.Empty => g,
            _ => value.ToString() is { } str and not "" ? new Subject(str) : null
        };

    static SubjectProperties FindSubjectProperties(Type type)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var idProperty = properties.FirstOrDefault(property =>
            string.Equals(property.Name, "Id", StringComparison.OrdinalIgnoreCase));

        // 1. Explicit [Subject] on property
        var explicitProperty = properties.FirstOrDefault(property =>
            property.IsDefined(typeof(SubjectAttribute), inherit: false));

        // 2. [Subject] on constructor parameter (record shorthand, without [property:])
        if (explicitProperty is null)
        {
            var primaryConstructor = type.GetConstructors().MaxBy(constructor => constructor.GetParameters().Length);
            var subjectParameter = primaryConstructor?.GetParameters()
                .FirstOrDefault(parameter => parameter.IsDefined(typeof(SubjectAttribute), inherit: false));

            if (subjectParameter is not null)
            {
                explicitProperty = properties.FirstOrDefault(property =>
                    string.Equals(property.Name, subjectParameter.Name, StringComparison.OrdinalIgnoreCase));
            }
        }

        return new(explicitProperty, idProperty == explicitProperty ? null : idProperty);
    }

    readonly record struct SubjectProperties(PropertyInfo? Explicit, PropertyInfo? Id);
}
