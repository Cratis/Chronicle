// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Projections.Graphs;

/// <summary>
/// Represents a member of a node or edge type: a parameter of its primary constructor or a property, with the attributes on it.
/// </summary>
/// <param name="Name">The CLR name of the member.</param>
/// <param name="Type">The CLR type of the member.</param>
/// <param name="Attributes">The attributes declared on the parameter and on the property of the same name.</param>
internal record GraphMember(string Name, Type Type, IReadOnlyList<Attribute> Attributes)
{
    /// <summary>
    /// Get the members of a type.
    /// </summary>
    /// <param name="type">The type to get the members of.</param>
    /// <returns>The members, the ones of the primary constructor first.</returns>
    public static IReadOnlyList<GraphMember> Of(Type type)
    {
        var members = new List<GraphMember>();
        var primaryConstructor = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).OrderByDescending(_ => _.GetParameters().Length).FirstOrDefault();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var parameter in primaryConstructor?.GetParameters() ?? [])
        {
            var property = properties.FirstOrDefault(_ => string.Equals(_.Name, parameter.Name, StringComparison.OrdinalIgnoreCase));
            members.Add(new(property?.Name ?? parameter.Name!, parameter.ParameterType, [.. parameter.GetCustomAttributes(), .. property?.GetCustomAttributes() ?? []]));
        }

        foreach (var property in properties.Where(_ => members.TrueForAll(m => !string.Equals(m.Name, _.Name, StringComparison.OrdinalIgnoreCase))))
        {
            members.Add(new(property.Name, property.PropertyType, [.. property.GetCustomAttributes()]));
        }

        return members;
    }

    /// <summary>
    /// Gets whether the member has an attribute.
    /// </summary>
    /// <typeparam name="TAttribute">The attribute type or interface.</typeparam>
    /// <returns>True if it has it, false if not.</returns>
    public bool Has<TAttribute>() => Attributes.OfType<TAttribute>().Any();
}
