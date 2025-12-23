// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Cratis.Chronicle.Storage.Security;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;

namespace Cratis.Chronicle.Server.Authentication.OpenIddict;

/// <summary>
/// OpenIddict application store backed by Chronicle's Security storage.
/// </summary>
/// <param name="applicationStorage">The application storage.</param>
public class ApplicationStore(IApplicationStorage applicationStorage) : IOpenIddictApplicationStore<Application>
{
    /// <inheritdoc/>
    public async ValueTask<long> CountAsync(CancellationToken cancellationToken)
    {
        var applications = await applicationStorage.GetAll();
        return applications.Count();
    }

    /// <inheritdoc/>
    public ValueTask<long> CountAsync<TResult>(Func<IQueryable<Application>, IQueryable<TResult>> query, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Queryable operations are not supported.");
    }

    /// <inheritdoc/>
    public async ValueTask CreateAsync(Application application, CancellationToken cancellationToken)
    {
        await applicationStorage.Create(application);
    }

    /// <inheritdoc/>
    public async ValueTask DeleteAsync(Application application, CancellationToken cancellationToken)
    {
        await applicationStorage.Delete(application.Id);
    }

    /// <inheritdoc/>
    public async ValueTask<Application?> FindByClientIdAsync(string identifier, CancellationToken cancellationToken)
    {
        return await applicationStorage.GetByClientId(identifier);
    }

    /// <inheritdoc/>
    public async ValueTask<Application?> FindByIdAsync(string identifier, CancellationToken cancellationToken)
    {
        return await applicationStorage.GetById(identifier);
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<Application> FindByPostLogoutRedirectUriAsync(string uri, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var applications = await applicationStorage.GetAll();
        foreach (var app in applications.Where(a => a.PostLogoutRedirectUris.Contains(uri)))
        {
            yield return app;
        }
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<Application> FindByRedirectUriAsync(string uri, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var applications = await applicationStorage.GetAll();
        foreach (var app in applications.Where(a => a.RedirectUris.Contains(uri)))
        {
            yield return app;
        }
    }

    /// <inheritdoc/>
    public ValueTask<TResult?> GetAsync<TState, TResult>(Func<IQueryable<Application>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Queryable operations are not supported.");
    }

    /// <inheritdoc/>
    public ValueTask<string?> GetApplicationTypeAsync(Application application, CancellationToken cancellationToken)
    {
        return new(application.Type);
    }

    /// <inheritdoc/>
    public ValueTask<string?> GetClientIdAsync(Application application, CancellationToken cancellationToken)
    {
        return new(application.ClientId);
    }

    /// <inheritdoc/>
    public ValueTask<string?> GetClientSecretAsync(Application application, CancellationToken cancellationToken)
    {
        return new(application.ClientSecret);
    }

    /// <inheritdoc/>
    public ValueTask<string?> GetClientTypeAsync(Application application, CancellationToken cancellationToken)
    {
        return new(application.Type);
    }

    /// <inheritdoc/>
    public ValueTask<string?> GetConsentTypeAsync(Application application, CancellationToken cancellationToken)
    {
        return new(application.ConsentType);
    }

    /// <inheritdoc/>
    public ValueTask<string?> GetDisplayNameAsync(Application application, CancellationToken cancellationToken)
    {
        return new(application.DisplayName);
    }

    /// <inheritdoc/>
    public ValueTask<ImmutableDictionary<CultureInfo, string>> GetDisplayNamesAsync(Application application, CancellationToken cancellationToken)
    {
        return new([]);
    }

    /// <inheritdoc/>
    public ValueTask<string?> GetIdAsync(Application application, CancellationToken cancellationToken)
    {
        return new(application.Id);
    }

    /// <inheritdoc/>
    public ValueTask<JsonWebKeySet?> GetJsonWebKeySetAsync(Application application, CancellationToken cancellationToken)
    {
        return new((JsonWebKeySet?)null);
    }

    /// <inheritdoc/>
    public ValueTask<ImmutableArray<string>> GetPermissionsAsync(Application application, CancellationToken cancellationToken)
    {
        return new(application.Permissions);
    }

    /// <inheritdoc/>
    public ValueTask<ImmutableArray<string>> GetPostLogoutRedirectUrisAsync(Application application, CancellationToken cancellationToken)
    {
        return new(application.PostLogoutRedirectUris);
    }

    /// <inheritdoc/>
    public ValueTask<ImmutableDictionary<string, JsonElement>> GetPropertiesAsync(Application application, CancellationToken cancellationToken)
    {
        return new(application.Properties);
    }

    /// <inheritdoc/>
    public ValueTask<ImmutableArray<string>> GetRedirectUrisAsync(Application application, CancellationToken cancellationToken)
    {
        return new(application.RedirectUris);
    }

    /// <inheritdoc/>
    public ValueTask<ImmutableArray<string>> GetRequirementsAsync(Application application, CancellationToken cancellationToken)
    {
        return new(application.Requirements);
    }

    /// <inheritdoc/>
    public ValueTask<ImmutableDictionary<string, string>> GetSettingsAsync(Application application, CancellationToken cancellationToken)
    {
        return new([]);
    }

    /// <inheritdoc/>
    public ValueTask<Application> InstantiateAsync(CancellationToken cancellationToken)
    {
        return new(new Application
        {
            Id = Guid.NewGuid().ToString()
        });
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<Application> ListAsync(int? count, int? offset, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var applications = await applicationStorage.GetAll();
        var query = applications.AsQueryable();

        if (offset.HasValue)
        {
            query = query.Skip(offset.Value);
        }

        if (count.HasValue)
        {
            query = query.Take(count.Value);
        }

        foreach (var app in query)
        {
            yield return app;
        }
    }

    /// <inheritdoc/>
    public IAsyncEnumerable<TResult> ListAsync<TState, TResult>(Func<IQueryable<Application>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Queryable operations are not supported.");
    }

    /// <inheritdoc/>
    public ValueTask SetApplicationTypeAsync(Application application, string? type, CancellationToken cancellationToken)
    {
        return default;
    }

    /// <inheritdoc/>
    public ValueTask SetClientIdAsync(Application application, string? identifier, CancellationToken cancellationToken)
    {
        application.ClientId = identifier ?? string.Empty;
        return default;
    }

    /// <inheritdoc/>
    public ValueTask SetClientSecretAsync(Application application, string? secret, CancellationToken cancellationToken)
    {
        application.ClientSecret = secret;
        return default;
    }

    /// <inheritdoc/>
    public ValueTask SetClientTypeAsync(Application application, string? type, CancellationToken cancellationToken)
    {
        application.Type = type;
        return default;
    }

    /// <inheritdoc/>
    public ValueTask SetConsentTypeAsync(Application application, string? type, CancellationToken cancellationToken)
    {
        application.ConsentType = type;
        return default;
    }

    /// <inheritdoc/>
    public ValueTask SetDisplayNameAsync(Application application, string? name, CancellationToken cancellationToken)
    {
        application.DisplayName = name;
        return default;
    }

    /// <inheritdoc/>
    public ValueTask SetDisplayNamesAsync(Application application, ImmutableDictionary<CultureInfo, string> names, CancellationToken cancellationToken)
    {
        // Display names not currently stored separately
        return default;
    }

    /// <inheritdoc/>
    public ValueTask SetJsonWebKeySetAsync(Application application, JsonWebKeySet? set, CancellationToken cancellationToken)
    {
        // JWK Set not currently stored
        return default;
    }

    /// <inheritdoc/>
    public ValueTask SetPermissionsAsync(Application application, ImmutableArray<string> permissions, CancellationToken cancellationToken)
    {
        application.Permissions = permissions;
        return default;
    }

    /// <inheritdoc/>
    public ValueTask SetPostLogoutRedirectUrisAsync(Application application, ImmutableArray<string> uris, CancellationToken cancellationToken)
    {
        application.PostLogoutRedirectUris = uris;
        return default;
    }

    /// <inheritdoc/>
    public ValueTask SetPropertiesAsync(Application application, ImmutableDictionary<string, JsonElement> properties, CancellationToken cancellationToken)
    {
        application.Properties = properties;
        return default;
    }

    /// <inheritdoc/>
    public ValueTask SetRedirectUrisAsync(Application application, ImmutableArray<string> uris, CancellationToken cancellationToken)
    {
        application.RedirectUris = uris;
        return default;
    }

    /// <inheritdoc/>
    public ValueTask SetRequirementsAsync(Application application, ImmutableArray<string> requirements, CancellationToken cancellationToken)
    {
        application.Requirements = requirements;
        return default;
    }

    /// <inheritdoc/>
    public ValueTask SetSettingsAsync(Application application, ImmutableDictionary<string, string> settings, CancellationToken cancellationToken)
    {
        // Settings not currently stored separately
        return default;
    }

    /// <inheritdoc/>
    public async ValueTask UpdateAsync(Application application, CancellationToken cancellationToken)
    {
        await applicationStorage.Update(application);
    }
}
