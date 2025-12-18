// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Cratis.Chronicle.Storage.Security;
using OpenIddict.Abstractions;

namespace Cratis.Chronicle.Server.Authentication.OpenIddict;

/// <summary>
/// In-memory scope store for OpenIddict. Scopes are predefined and not persisted.
/// </summary>
public class ScopeStore : IOpenIddictScopeStore<Scope>
{
    static readonly ConcurrentDictionary<string, Scope> _scopes = new();

    static ScopeStore()
    {
        var apiScope = new Scope(
            Id: "api",
            Name: "api",
            DisplayName: "Chronicle API",
            Description: "Access to Chronicle API",
            Resources: [],
            Properties: []);
        _scopes[apiScope.Id] = apiScope;
    }

    /// <inheritdoc/>
    public ValueTask<long> CountAsync(CancellationToken cancellationToken) => new(_scopes.Count);

    /// <inheritdoc/>
    public ValueTask<long> CountAsync<TResult>(Func<IQueryable<Scope>, IQueryable<TResult>> query, CancellationToken cancellationToken) => throw new NotSupportedException("Queryable operations are not supported.");

    /// <inheritdoc/>
    public ValueTask CreateAsync(Scope scope, CancellationToken cancellationToken)
    {
        _scopes[scope.Id] = scope;
        return default;
    }

    /// <inheritdoc/>
    public ValueTask DeleteAsync(Scope scope, CancellationToken cancellationToken)
    {
        _scopes.TryRemove(scope.Id, out _);
        return default;
    }

    /// <inheritdoc/>
    public ValueTask<Scope?> FindByIdAsync(string identifier, CancellationToken cancellationToken)
    {
        _scopes.TryGetValue(identifier, out var scope);
        return new(scope);
    }

    /// <inheritdoc/>
    public ValueTask<Scope?> FindByNameAsync(string name, CancellationToken cancellationToken) => new(_scopes.Values.FirstOrDefault(s => s.Name == name));

    /// <inheritdoc/>
    public async IAsyncEnumerable<Scope> FindByNamesAsync(ImmutableArray<string> names, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var name in names)
        {
            var scope = _scopes.Values.FirstOrDefault(s => s.Name == name);
            if (scope is not null)
            {
                yield return scope;
            }
        }
        await ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<Scope> FindByResourceAsync(string resource, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var scope in _scopes.Values)
        {
            if (scope.Resources.Contains(resource))
            {
                yield return scope;
            }
        }
        await ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public ValueTask<TResult?> GetAsync<TState, TResult>(Func<IQueryable<Scope>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken) => throw new NotSupportedException("Queryable operations are not supported.");

    /// <inheritdoc/>
    public ValueTask<string?> GetDescriptionAsync(Scope scope, CancellationToken cancellationToken) => new(scope.Description);

    /// <inheritdoc/>
    public ValueTask<ImmutableDictionary<CultureInfo, string>> GetDescriptionsAsync(Scope scope, CancellationToken cancellationToken) => new([]);

    /// <inheritdoc/>
    public ValueTask<string?> GetDisplayNameAsync(Scope scope, CancellationToken cancellationToken) => new(scope.DisplayName);

    /// <inheritdoc/>
    public ValueTask<ImmutableDictionary<CultureInfo, string>> GetDisplayNamesAsync(Scope scope, CancellationToken cancellationToken) => new([]);

    /// <inheritdoc/>
    public ValueTask<string?> GetIdAsync(Scope scope, CancellationToken cancellationToken) => new(scope.Id);

    /// <inheritdoc/>
    public ValueTask<string?> GetNameAsync(Scope scope, CancellationToken cancellationToken) => new(scope.Name);

    /// <inheritdoc/>
    public ValueTask<ImmutableDictionary<string, JsonElement>> GetPropertiesAsync(Scope scope, CancellationToken cancellationToken) => new(scope.Properties);

    /// <inheritdoc/>
    public ValueTask<ImmutableArray<string>> GetResourcesAsync(Scope scope, CancellationToken cancellationToken) => new(scope.Resources);

    /// <inheritdoc/>
    public ValueTask<Scope> InstantiateAsync(CancellationToken cancellationToken) => new(new Scope(
        Id: Guid.NewGuid().ToString(),
        Name: string.Empty,
        DisplayName: null,
        Description: null,
        Resources: [],
        Properties: []));

    /// <inheritdoc/>
    public async IAsyncEnumerable<Scope> ListAsync(int? count, int? offset, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var scopeList = _scopes.Values
            .Skip(offset ?? 0)
            .Take(count ?? 100);

        foreach (var scope in scopeList)
        {
            yield return scope;
        }
        await ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public IAsyncEnumerable<TResult> ListAsync<TState, TResult>(Func<IQueryable<Scope>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken) => throw new NotSupportedException("Queryable operations are not supported.");

    /// <inheritdoc/>
    public async ValueTask SetDescriptionAsync(Scope scope, string? description, CancellationToken cancellationToken)
    {
        _scopes[scope.Id] = scope with { Description = description };
        await ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public ValueTask SetDescriptionsAsync(Scope scope, ImmutableDictionary<CultureInfo, string> descriptions, CancellationToken cancellationToken) => default;

    /// <inheritdoc/>
    public async ValueTask SetDisplayNameAsync(Scope scope, string? name, CancellationToken cancellationToken)
    {
        _scopes[scope.Id] = scope with { DisplayName = name };
        await ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public ValueTask SetDisplayNamesAsync(Scope scope, ImmutableDictionary<CultureInfo, string> names, CancellationToken cancellationToken) => default;

    /// <inheritdoc/>
    public async ValueTask SetNameAsync(Scope scope, string? name, CancellationToken cancellationToken)
    {
        _scopes[scope.Id] = scope with { Name = name ?? string.Empty };
        await ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public async ValueTask SetPropertiesAsync(Scope scope, ImmutableDictionary<string, JsonElement> properties, CancellationToken cancellationToken)
    {
        _scopes[scope.Id] = scope with { Properties = properties };
        await ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public async ValueTask SetResourcesAsync(Scope scope, ImmutableArray<string> resources, CancellationToken cancellationToken)
    {
        _scopes[scope.Id] = scope with { Resources = resources };
        await ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public ValueTask UpdateAsync(Scope scope, CancellationToken cancellationToken)
    {
        _scopes[scope.Id] = scope;
        return default;
    }
}
