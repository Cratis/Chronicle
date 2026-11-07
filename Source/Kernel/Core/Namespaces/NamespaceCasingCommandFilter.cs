// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Security;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Namespaces;

/// <summary>
/// Resolves kernel command namespaces after authorization and routing validation.
/// </summary>
/// <param name="services">Services from which to resolve casing only for kernel namespace commands.</param>
public class NamespaceCasingCommandFilter(IServiceProvider services) : ICommandFilter
{
    static readonly ConcurrentDictionary<Type, PropertyInfo[]> _properties = new();
    static readonly ConcurrentDictionary<Type, Action<object, EventStoreNamespaceName>> _setters = new();
    readonly SystemStoreCommandFilter _routingValidation = new();

    /// <inheritdoc/>
    public async Task<CommandResult> OnExecution(CommandContext context)
    {
        var routing = KernelCommandRouting.For(context);
        var eventStore = routing.FirstOrDefault(_ => _.Name == "EventStore").Get?.Invoke(context.Command);
        var @namespace = routing.FirstOrDefault(_ => _.Name == "Namespace").Get?.Invoke(context.Command);
        if (eventStore is null || @namespace is null)
        {
            return null!;
        }

        // Authorization filters run first. Ordinary filter discovery order is unspecified, so do not resolve a
        // namespace before separator validation even if this filter is discovered before the routing filter.
        var validation = await _routingValidation.OnExecution(context);
        if (validation?.IsSuccess == false) return validation;

        var properties = _properties.GetOrAdd(context.Command.GetType(), type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(_ => _.Name == "Namespace" && _.SetMethod is not null && (_.PropertyType == typeof(string) || _.PropertyType == typeof(EventStoreNamespaceName))).ToArray());
        var property = properties.FirstOrDefault();
        if (property is null) return null!;

        var casing = (context.ServiceProvider ?? services).GetRequiredService<INamespaceCasing>();
        var canonical = await casing.Canonicalize(eventStore, @namespace);
        if (!string.Equals(canonical.Value, @namespace, StringComparison.Ordinal))
        {
            var setter = _setters.GetOrAdd(
                context.Command.GetType(),
                static (type, property) =>
            {
                var command = Expression.Parameter(typeof(object));
                var value = Expression.Parameter(typeof(EventStoreNamespaceName));
                Expression argument = property.PropertyType == typeof(string) ? Expression.Property(value, nameof(EventStoreNamespaceName.Value)) : value;
                return Expression.Lambda<Action<object, EventStoreNamespaceName>>(
                    Expression.Call(Expression.Convert(command, type), property.SetMethod!, argument), command, value).Compile();
            },
                property);
            setter(context.Command, canonical);
        }
        return null!;
    }
}
