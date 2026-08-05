// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Locates the property a read model uses as its document identifier.
/// </summary>
/// <remarks>
/// One definition of the precedence MongoDB maps to <c>_id</c>, shared by the harness code that mirrors that
/// mapping and by the code that reports the mapping as substituted — so the two cannot disagree about which
/// property the document is keyed by.
/// </remarks>
internal static class IdentifierProperty
{
    /// <summary>
    /// Finds the identifier property of a read model, following the same precedence MongoDB uses to map to
    /// <c>_id</c>:
    /// <list type="number">
    /// <item><description><see cref="KeyAttribute"/> on a property or record positional parameter.</description></item>
    /// <item><description><see cref="SubjectAttribute"/> on a property or record positional parameter.</description></item>
    /// <item><description>A property literally named <c>Id</c> (MongoDB default-id convention).</description></item>
    /// </list>
    /// </summary>
    /// <param name="readModelType">The read model CLR type to inspect.</param>
    /// <returns>The identifier <see cref="PropertyInfo"/>, or <see langword="null"/> when the read model has none.</returns>
    public static PropertyInfo? Find(Type readModelType)
    {
        var properties = readModelType.GetProperties();
        var primaryCtor = readModelType.GetConstructors().FirstOrDefault();
        var parameters = primaryCtor?.GetParameters() ?? [];

        return FindByAttribute<KeyAttribute>(properties, parameters)
            ?? FindByAttribute<SubjectAttribute>(properties, parameters)
            ?? properties.FirstOrDefault(p => string.Equals(p.Name, "Id", StringComparison.Ordinal));
    }

    static PropertyInfo? FindByAttribute<TAttribute>(PropertyInfo[] properties, ParameterInfo[] parameters)
        where TAttribute : Attribute
    {
        var taggedProperty = properties.FirstOrDefault(p => Attribute.IsDefined(p, typeof(TAttribute)));
        if (taggedProperty is not null) return taggedProperty;

        var taggedParameter = parameters.FirstOrDefault(p => Attribute.IsDefined(p, typeof(TAttribute)));
        return taggedParameter is null
            ? null
            : properties.FirstOrDefault(p => string.Equals(p.Name, taggedParameter.Name, StringComparison.Ordinal));
    }
}
