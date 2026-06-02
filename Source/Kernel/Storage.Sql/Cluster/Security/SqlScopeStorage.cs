// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Chronicle.Storage.Security;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Cluster.Security;

/// <summary>
/// SQL implementation of <see cref="IScopeStorage"/>.
/// </summary>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
/// <param name="jsonSerializerOptions">The configured <see cref="JsonSerializerOptions"/>.</param>
public class SqlScopeStorage(IDatabase database, JsonSerializerOptions jsonSerializerOptions) : IScopeStorage
{
    /// <inheritdoc/>
    public async Task<Scope?> GetById(string id, CancellationToken cancellationToken = default)
    {
        await using var dbScope = await database.Cluster();
        var entity = await dbScope.DbContext.Scopes.FindAsync([id], cancellationToken);
        return entity is null ? null : ToScope(entity);
    }

    /// <inheritdoc/>
    public async Task<Scope?> GetByName(string name, CancellationToken cancellationToken = default)
    {
        await using var dbScope = await database.Cluster();
        var entity = await dbScope.DbContext.Scopes.FirstOrDefaultAsync(s => s.Name == name, cancellationToken);
        return entity is null ? null : ToScope(entity);
    }

    /// <inheritdoc/>
    public async Task Create(Scope scope, CancellationToken cancellationToken = default)
    {
        await using var dbScope = await database.Cluster();
        dbScope.DbContext.Scopes.Add(ToEntity(scope));
        await dbScope.DbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task Update(Scope scope, CancellationToken cancellationToken = default)
    {
        await using var dbScope = await database.Cluster();
        var entity = await dbScope.DbContext.Scopes.FindAsync([scope.Id], cancellationToken);
        if (entity is not null)
        {
            UpdateFrom(entity, scope);
            await dbScope.DbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            dbScope.DbContext.Scopes.Add(ToEntity(scope));
            await dbScope.DbContext.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task Delete(string id, CancellationToken cancellationToken = default)
    {
        await using var dbScope = await database.Cluster();
        var entity = await dbScope.DbContext.Scopes.FindAsync([id], cancellationToken);
        if (entity is not null)
        {
            dbScope.DbContext.Scopes.Remove(entity);
            await dbScope.DbContext.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task<long> Count(CancellationToken cancellationToken = default)
    {
        await using var dbScope = await database.Cluster();
        return await dbScope.DbContext.Scopes.LongCountAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Scope>> List(int? count, int? offset, CancellationToken cancellationToken = default)
    {
        await using var dbScope = await database.Cluster();
        var query = dbScope.DbContext.Scopes.AsQueryable();
        if (offset.HasValue)
        {
            query = query.Skip(offset.Value);
        }

        if (count.HasValue)
        {
            query = query.Take(count.Value);
        }

        return (await query.ToListAsync(cancellationToken)).Select(ToScope).ToArray();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Scope>> FindByResource(string resource, CancellationToken cancellationToken = default)
    {
        await using var dbScope = await database.Cluster();
        var all = await dbScope.DbContext.Scopes.ToListAsync(cancellationToken);
        return all.Select(ToScope).Where(s => s.Resources.Contains(resource)).ToArray();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Scope>> FindByNames(IEnumerable<string> names, CancellationToken cancellationToken = default)
    {
        var nameSet = names.ToHashSet();
        await using var dbScope = await database.Cluster();
        return (await dbScope.DbContext.Scopes.Where(s => s.Name != null && nameSet.Contains(s.Name)).ToListAsync(cancellationToken)).Select(ToScope).ToArray();
    }

    static ScopeEntity ToEntity(Scope scope) => new()
    {
        Id = scope.Id,
        Name = scope.Name,
        DisplayName = scope.DisplayName,
        Description = scope.Description,
    };

    static void UpdateFrom(ScopeEntity entity, Scope scope)
    {
        entity.Name = scope.Name;
        entity.DisplayName = scope.DisplayName;
        entity.Description = scope.Description;
    }

    Scope ToScope(ScopeEntity entity)
    {
        var resources = ImmutableArray<string>.Empty;
        if (!string.IsNullOrEmpty(entity.Resources))
        {
            try
            {
                var resourceArray = JsonSerializer.Deserialize<string[]>(entity.Resources, jsonSerializerOptions) ?? [];
                resources = [.. resourceArray];
            }
            catch
            {
                // Ignore deserialization errors
            }
        }

        var properties = ImmutableDictionary<string, JsonElement>.Empty;
        if (!string.IsNullOrEmpty(entity.Properties))
        {
            try
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(entity.Properties, jsonSerializerOptions);
                if (dict is not null)
                {
                    properties = dict.ToImmutableDictionary();
                }
            }
            catch
            {
                // Ignore deserialization errors
            }
        }

        return new Scope(entity.Id, entity.Name, entity.DisplayName, entity.Description, resources, properties);
    }
}
