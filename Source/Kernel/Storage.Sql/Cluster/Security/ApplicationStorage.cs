// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Storage.Security;
using Microsoft.EntityFrameworkCore;
using ApplicationId = Cratis.Chronicle.Concepts.Security.ApplicationId;

namespace Cratis.Chronicle.Storage.Sql.Cluster.Security;

/// <summary>
/// Represents an implementation of <see cref="IApplicationStorage"/> for SQL.
/// </summary>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
public class ApplicationStorage(IDatabase database) : IApplicationStorage, IDisposable
{
    readonly BehaviorSubject<IEnumerable<Application>> _subject = new([]);

    /// <inheritdoc/>
    public ISubject<IEnumerable<Application>> ObserveAll()
    {
        Task.Run(async () =>
        {
            var applications = await GetAll();
            _subject.OnNext(applications);
        });
        return _subject;
    }

    /// <inheritdoc/>
    public async Task<Application?> GetById(ApplicationId id, CancellationToken cancellationToken = default)
    {
        await using var scope = await database.Cluster();
        var entity = await scope.DbContext.Applications.FindAsync([id.Value], cancellationToken);
        return entity is null ? null : ToApplication(entity);
    }

    /// <inheritdoc/>
    public async Task<Application?> GetByClientId(ClientId clientId, CancellationToken cancellationToken = default)
    {
        await using var scope = await database.Cluster();
        var entity = await scope.DbContext.Applications.FirstOrDefaultAsync(a => a.ClientId == clientId.Value, cancellationToken);
        return entity is null ? null : ToApplication(entity);
    }

    /// <inheritdoc/>
    public async Task Create(Application application, CancellationToken cancellationToken = default)
    {
        await using var scope = await database.Cluster();
        scope.DbContext.Applications.Add(ToEntity(application));
        await scope.DbContext.SaveChangesAsync(cancellationToken);
        await RefreshSubject();
    }

    /// <inheritdoc/>
    public async Task Update(Application application, CancellationToken cancellationToken = default)
    {
        await using var scope = await database.Cluster();
        await scope.DbContext.Applications.Upsert(ToEntity(application));
        await scope.DbContext.SaveChangesAsync(cancellationToken);
        await RefreshSubject();
    }

    /// <inheritdoc/>
    public async Task Delete(ApplicationId id, CancellationToken cancellationToken = default)
    {
        await using var scope = await database.Cluster();
        var entity = await scope.DbContext.Applications.FindAsync([id.Value], cancellationToken);
        if (entity is not null)
        {
            scope.DbContext.Applications.Remove(entity);
            await scope.DbContext.SaveChangesAsync(cancellationToken);
        }
        await RefreshSubject();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Application>> GetAll(CancellationToken cancellationToken = default)
    {
        await using var scope = await database.Cluster();
        var entities = await scope.DbContext.Applications.ToListAsync(cancellationToken);
        return entities.Select(ToApplication).ToArray();
    }

    /// <inheritdoc/>
    public async Task<long> Count(CancellationToken cancellationToken = default)
    {
        await using var scope = await database.Cluster();
        return await scope.DbContext.Applications.LongCountAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Application>> List(int? count, int? offset, CancellationToken cancellationToken = default)
    {
        await using var scope = await database.Cluster();
        var query = scope.DbContext.Applications.AsQueryable();
        if (offset.HasValue)
        {
            query = query.Skip(offset.Value);
        }
        if (count.HasValue)
        {
            query = query.Take(count.Value);
        }
        var entities = await query.ToListAsync(cancellationToken);
        return entities.Select(ToApplication).ToArray();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Application>> FindByRedirectUri(string redirectUri, CancellationToken cancellationToken = default)
    {
        var all = await GetAll(cancellationToken);
        return all.Where(a => a.RedirectUris.Any(r => r.Value == redirectUri)).ToArray();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Application>> FindByPostLogoutRedirectUri(string postLogoutRedirectUri, CancellationToken cancellationToken = default)
    {
        var all = await GetAll(cancellationToken);
        return all.Where(a => a.PostLogoutRedirectUris.Any(r => r.Value == postLogoutRedirectUri)).ToArray();
    }

    /// <inheritdoc/>
    public void Dispose() => _subject.Dispose();

    static Application ToApplication(ApplicationEntity entity)
    {
        ImmutableDictionary<PropertyName, JsonElement> properties = [];
        if (!string.IsNullOrEmpty(entity.Properties))
        {
            try
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(entity.Properties);
                if (dict is not null)
                {
                    properties = dict.ToImmutableDictionary(
                        kvp => (PropertyName)kvp.Key,
                        kvp => kvp.Value);
                }
            }
            catch
            {
                // Ignore deserialization errors
            }
        }

        return new Application
        {
            Id = (ApplicationId)entity.Id,
            ClientId = (ClientId)entity.ClientId,
            ClientSecret = entity.ClientSecret is null ? null : (ClientSecret)entity.ClientSecret,
            DisplayName = entity.DisplayName is null ? null : (ApplicationDisplayName)entity.DisplayName,
            Type = entity.Type is null ? null : (ApplicationType)entity.Type,
            ConsentType = entity.ConsentType is null ? null : (ConsentType)entity.ConsentType,
            Permissions = entity.Permissions.Select(p => (Permission)p).ToImmutableArray(),
            Requirements = entity.Requirements.Select(r => (Requirement)r).ToImmutableArray(),
            RedirectUris = entity.RedirectUris.Select(r => (RedirectUri)r).ToImmutableArray(),
            PostLogoutRedirectUris = entity.PostLogoutRedirectUris.Select(r => (RedirectUri)r).ToImmutableArray(),
            Properties = properties,
        };
    }

    static ApplicationEntity ToEntity(Application application) => new()
    {
        Id = application.Id.Value,
        ClientId = application.ClientId.Value,
        ClientSecret = application.ClientSecret?.Value,
        DisplayName = application.DisplayName?.Value,
        Type = application.Type?.Value,
        ConsentType = application.ConsentType?.Value,
        Permissions = application.Permissions.Select(p => p.Value).ToArray(),
        Requirements = application.Requirements.Select(r => r.Value).ToArray(),
        RedirectUris = application.RedirectUris.Select(r => r.Value).ToArray(),
        PostLogoutRedirectUris = application.PostLogoutRedirectUris.Select(r => r.Value).ToArray(),
        Properties = application.Properties.IsEmpty
            ? null
            : JsonSerializer.Serialize(application.Properties.ToDictionary(kvp => kvp.Key.Value, kvp => kvp.Value)),
    };

    async Task RefreshSubject()
    {
        var applications = await GetAll();
        _subject.OnNext(applications);
    }
}
