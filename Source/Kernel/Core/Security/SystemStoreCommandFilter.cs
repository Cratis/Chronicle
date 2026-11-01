// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Restricts the System event store to administrators for every command the kernel handles, and refuses routing
/// values that contain the grain key separator.
/// </summary>
/// <remarks>
/// Commands from other assemblies are ignored. This filter never grants anything: it only refuses.
/// </remarks>
public class SystemStoreCommandFilter : ICommandFilter
{
    static readonly Task<CommandResult> _noVerdict = Task.FromResult<CommandResult>(null!);
    static readonly ConcurrentDictionary<Type, RoutingGetter[]> _getters = new();
    static readonly Assembly _kernelAssembly = typeof(SystemStoreCommandFilter).Assembly;

    /// <inheritdoc/>
    public Task<CommandResult> OnExecution(CommandContext context)
    {
        if (context.Type.Assembly != _kernelAssembly) return _noVerdict;

        var getters = _getters.GetOrAdd(context.Type, static type => CreateGetters(type));
        if (getters.Length == 0) return _noVerdict;

        var targetsSystem = false;
        foreach (var getter in getters)
        {
            var value = getter.Get(context.Command);
            if (SystemStoreAccess.HasKeySeparator(value))
            {
                return Task.FromResult(Separator(context, getter.Name));
            }

            targetsSystem |= getter.IsStore && SystemStoreAccess.IsSystem(value);
        }

        return targetsSystem ? AuthorizeSystem(context) : _noVerdict;
    }

    static async Task<CommandResult> AuthorizeSystem(CommandContext context)
    {
        if (context.ServiceProvider is { } services)
        {
            var access = services.GetRequiredService<ISystemStoreAccess>();
            if (await access.IsAllowed(SystemStoreRouting.CallerFor(services, context.AuthorizedPrincipal))) return null!;
        }

        return CommandResult.Unauthorized(context.CorrelationId, SystemStoreAccess.Refused);
    }

    static CommandResult Separator(CommandContext context, string member)
    {
        var result = CommandResult.Success(context.CorrelationId);
        result.ValidationResults = [ValidationResult.Error(SystemStoreAccess.SeparatorRefused, [member])];
        return result;
    }

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

    readonly record struct RoutingGetter(string Name, bool IsStore, Func<object, string?> Get);
}
