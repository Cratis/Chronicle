// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Cratis.Chronicle.Concepts;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Security;

/// <summary>
/// The trusted identity, scope, and destructive classification of an administrator operation.
/// </summary>
internal sealed class OperationAuthorizationMetadata
{
    OperationAuthorizationMetadata(string name, AuthorizationScope scope, bool isDestructive)
    {
        Name = name;
        Scope = scope;
        IsDestructive = isDestructive;
    }

    /// <summary>
    /// Gets the trusted operation identity.
    /// </summary>
    internal string Name { get; }

    /// <summary>
    /// Gets the operation's effective scope.
    /// </summary>
    internal AuthorizationScope Scope { get; }

    /// <summary>
    /// Gets whether the operation discards or rewrites data.
    /// </summary>
    internal bool IsDestructive { get; }

    /// <summary>
    /// Resolves metadata without invoking the operation, refusing unresolvable query declarations or arguments.
    /// </summary>
    /// <param name="resource">The original authorization resource.</param>
    /// <returns>The metadata, or null when a query cannot be resolved safely.</returns>
    internal static OperationAuthorizationMetadata? From(object? resource) => resource switch
    {
        CommandContext command => ForOperation(command.Command),
        QueryContext query => ForQuery(query),
        _ => ForOperation(resource)
    };

    static OperationAuthorizationMetadata ForOperation(object? operation)
    {
        var value = operation?.GetType().GetProperty("EventStore")?.GetValue(operation);
        var store = value is EventStoreName name ? name.Value : value as string;
        var scope = string.IsNullOrWhiteSpace(store) ? AuthorizationScope.Kernel : AuthorizationScope.ForEventStore(store);
        var destructive = operation?.GetType().IsDefined(typeof(DestructiveOperationAttribute), false) is true;
        return new(operation?.GetType().Name ?? "unknown", scope, destructive);
    }

    static OperationAuthorizationMetadata? ForQuery(QueryContext query)
    {
        var providers = query.ServiceProvider?.GetService<IQueryPerformerProviders>();
        if (providers is null || !providers.TryGetPerformersFor(query.Name, out var performer)) return null;
        if (performer is not IAuthorizationQueryTarget { AuthorizationMethod: { } method }) return null;
        if (performer.FullyQualifiedName != query.Name) return null;
        var scope = QueryScope(query, method, performer.Parameters.ToArray());
        if (scope is null) return null;

        var destructive = method.IsDefined(typeof(DestructiveOperationAttribute), false) ||
            method.DeclaringType?.IsDefined(typeof(DestructiveOperationAttribute), false) == true;
        return new(performer.FullyQualifiedName.Value, scope, destructive);
    }

    static AuthorizationScope? QueryScope(QueryContext query, MethodInfo method, QueryParameter[] parameters)
    {
        var storeParameters = parameters.Where(_ => string.Equals(_.Name, "eventStore", StringComparison.OrdinalIgnoreCase)).ToArray();
        if (storeParameters.Length == 0) return AuthorizationScope.Kernel;
        if (storeParameters.Length != 1 || HasAdditionalStoreParameter(parameters, storeParameters[0])) return null;

        var declared = storeParameters[0];
        var parameter = method.GetParameters().SingleOrDefault(_ => _.Name == declared.Name && _.ParameterType == declared.Type);
        if (parameter is null || !IsStoreType(parameter.ParameterType)) return null;

        return BoundStoreScope(query, parameter);
    }

    static bool HasAdditionalStoreParameter(QueryParameter[] parameters, QueryParameter storeParameter) =>
        parameters.Any(_ => _.Name != storeParameter.Name && _.Type == typeof(EventStoreName));

    static AuthorizationScope? BoundStoreScope(QueryContext query, ParameterInfo parameter)
    {
        var values = query.Arguments?.Where(_ => string.Equals(_.Key, parameter.Name, StringComparison.OrdinalIgnoreCase)).ToArray() ?? [];
        if (values.Length > 1) return null;
        var value = values.Length == 1 ? values[0].Value : parameter.DefaultValue;
        return ValidatedStoreScope(value);
    }

    static bool IsStoreType(Type type) => type == typeof(string) || type == typeof(EventStoreName);

    static AuthorizationScope? ValidatedStoreScope(object? value)
    {
        var store = value switch
        {
            string name => name,
            EventStoreName name => name.Value,
            _ => null
        };
        if (string.IsNullOrWhiteSpace(store) || SystemStoreAccess.HasKeySeparator(store)) return null;
        return AuthorizationScope.ForEventStore(store);
    }
}
