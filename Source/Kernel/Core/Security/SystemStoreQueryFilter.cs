// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Arc.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Restricts the System event store to administrators for every kernel query, and refuses routing values that
/// contain the grain key separator.
/// </summary>
/// <remarks>
/// Queries from other namespaces are ignored. This filter never grants anything: it only refuses.
/// </remarks>
public class SystemStoreQueryFilter : IQueryFilter
{
    const string KernelQueryPrefix = "Cratis.Chronicle.";

    static readonly Task<QueryResult> _noVerdict = Task.FromResult<QueryResult>(null!);

    /// <inheritdoc/>
    public Task<QueryResult> OnPerform(QueryContext context)
    {
        if (context.Arguments is not { Count: > 0 } arguments) return _noVerdict;
        if (!context.Name.Value.StartsWith(KernelQueryPrefix, StringComparison.Ordinal)) return _noVerdict;

        var targetsSystem = false;
        foreach (var argument in arguments)
        {
            var name = RoutingName(argument.Key);
            if (name is null) continue;

            var value = SystemStoreRouting.AsString(argument.Value);
            if (SystemStoreAccess.HasKeySeparator(value))
            {
                return Task.FromResult(Separator(context, name));
            }

            targetsSystem |= SystemStoreRouting.IsStoreName(name) && SystemStoreAccess.IsSystem(value);
        }

        return targetsSystem ? AuthorizeSystem(context) : _noVerdict;
    }

    static string? RoutingName(string key)
    {
        foreach (var name in SystemStoreRouting.Names)
        {
            if (string.Equals(name, key, StringComparison.OrdinalIgnoreCase)) return name;
        }

        return null;
    }

    static async Task<QueryResult> AuthorizeSystem(QueryContext context)
    {
        if (context.ServiceProvider is { } services)
        {
            var access = services.GetRequiredService<ISystemStoreAccess>();
            if (await access.IsAllowed(SystemStoreRouting.CallerFor(services, context.AuthorizedPrincipal))) return null!;
        }

        return QueryResult.Unauthorized(context.CorrelationId);
    }

    static QueryResult Separator(QueryContext context, string member)
    {
        var result = QueryResult.Success(context.CorrelationId);
        result.ValidationResults = [ValidationResult.Error(SystemStoreAccess.SeparatorRefused, [member])];
        return result;
    }
}
