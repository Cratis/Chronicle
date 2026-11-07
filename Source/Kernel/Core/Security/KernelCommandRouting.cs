// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Cratis.Arc.Commands;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Reads the routing values of the commands the kernel handles.
/// </summary>
/// <remarks>
/// Commands from other assemblies have no routing values here.
/// </remarks>
static class KernelCommandRouting
{
    static readonly ConcurrentDictionary<Type, RoutingGetter[]> _getters = new();
    static readonly Assembly _kernelAssembly = typeof(KernelCommandRouting).Assembly;

    /// <summary>
    /// Gets the routing value getters for the command being executed.
    /// </summary>
    /// <param name="context">The <see cref="CommandContext"/> of the command.</param>
    /// <returns>The getters, empty if it is not a kernel command or has no routing values.</returns>
    internal static RoutingGetter[] For(CommandContext context) => For(context.Type);

    /// <summary>
    /// Gets the routing value getters for a command type.
    /// </summary>
    /// <param name="type">The type of the command.</param>
    /// <returns>The getters, empty if it is not a kernel command or has no routing values.</returns>
    internal static RoutingGetter[] For(Type type) => type.Assembly == _kernelAssembly ? GettersFor(type) : [];

    /// <summary>
    /// Gets the routing value getters for a type, without regard to the assembly it is declared in.
    /// </summary>
    /// <param name="type">The type of the command.</param>
    /// <returns>The getters.</returns>
    internal static RoutingGetter[] GettersFor(Type type) => _getters.GetOrAdd(type, static key => CreateGetters(key));

    static RoutingGetter[] CreateGetters(Type type)
    {
        var getters = new List<RoutingGetter>();
        foreach (var name in SystemStoreRouting.Names)
        {
            var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (property is null || property.GetIndexParameters().Length > 0 || !SystemStoreRouting.IsStringLike(property.PropertyType)) continue;
            getters.Add(new RoutingGetter(name, SystemStoreRouting.IsStoreName(name), Compile(type, property)));
        }

        return [.. getters];
    }

    static Func<object, string?> Compile(Type type, PropertyInfo property)
    {
        var parameter = Expression.Parameter(typeof(object));
        Expression read = Expression.Property(Expression.Convert(parameter, type), property);
        if (property.PropertyType != typeof(string))
        {
            var concept = Expression.Variable(typeof(ConceptAs<string>));
            read = Expression.Block(
                [concept],
                Expression.Assign(concept, Expression.Convert(read, typeof(ConceptAs<string>))),
                Expression.Condition(
                    Expression.Equal(concept, Expression.Constant(null, typeof(ConceptAs<string>))),
                    Expression.Constant(null, typeof(string)),
                    Expression.Property(concept, nameof(ConceptAs<string>.Value))));
        }

        return Expression.Lambda<Func<object, string?>>(read, parameter).Compile();
    }

    /// <summary>
    /// A compiled reader of one routing value of a command.
    /// </summary>
    /// <param name="Name">The name of the routing member.</param>
    /// <param name="IsStore">Whether the member names an event store.</param>
    /// <param name="Get">Reads the value from a command.</param>
    internal readonly record struct RoutingGetter(string Name, bool IsStore, Func<object, string?> Get);
}
