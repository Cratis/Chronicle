// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Storage.Security;
using Microsoft.EntityFrameworkCore;
using ApplicationId = Cratis.Chronicle.Concepts.Security.ApplicationId;

namespace Cratis.Chronicle.Storage.Sql.Cluster.Security;

/// <summary>
/// SQL implementation of <see cref="IAuthorizationStorage"/>.
/// </summary>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
/// <param name="jsonSerializerOptions">The configured <see cref="JsonSerializerOptions"/>.</param>
public class SqlAuthorizationStorage(IDatabase database, JsonSerializerOptions jsonSerializerOptions) : IAuthorizationStorage
{
    /// <inheritdoc/>
    public async Task<Authorization?> GetById(AuthorizationId id, CancellationToken cancellationToken = default)
    {
        await using var scope = await database.Cluster();
        var entity = await scope.DbContext.Authorizations.FindAsync([id.Value], cancellationToken);
        return entity is null ? null : ToAuthorization(entity);
    }

    /// <inheritdoc/>
    public async Task Create(Authorization authorization, CancellationToken cancellationToken = default)
    {
        await using var scope = await database.Cluster();
        scope.DbContext.Authorizations.Add(ToEntity(authorization));
        await scope.DbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task Update(Authorization authorization, CancellationToken cancellationToken = default)
    {
        await using var scope = await database.Cluster();
        var entity = await scope.DbContext.Authorizations.FindAsync([authorization.Id.Value], cancellationToken);
        if (entity is not null)
        {
            UpdateFrom(entity, authorization);
            await scope.DbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            scope.DbContext.Authorizations.Add(ToEntity(authorization));
            await scope.DbContext.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task Delete(AuthorizationId id, CancellationToken cancellationToken = default)
    {
        await using var scope = await database.Cluster();
        var entity = await scope.DbContext.Authorizations.FindAsync([id.Value], cancellationToken);
        if (entity is not null)
        {
            scope.DbContext.Authorizations.Remove(entity);
            await scope.DbContext.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task<long> Count(CancellationToken cancellationToken = default)
    {
        await using var scope = await database.Cluster();
        return await scope.DbContext.Authorizations.LongCountAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Authorization>> List(int? count, int? offset, CancellationToken cancellationToken = default)
    {
        await using var scope = await database.Cluster();
        var query = scope.DbContext.Authorizations.AsQueryable();
        if (offset.HasValue)
        {
            query = query.Skip(offset.Value);
        }

        if (count.HasValue)
        {
            query = query.Take(count.Value);
        }

        return (await query.ToListAsync(cancellationToken)).Select(ToAuthorization).ToArray();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Authorization>> FindByApplicationId(ApplicationId applicationId, CancellationToken cancellationToken = default)
    {
        await using var scope = await database.Cluster();
        return (await scope.DbContext.Authorizations.Where(a => a.ApplicationId == applicationId.Value).ToListAsync(cancellationToken)).Select(ToAuthorization).ToArray();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Authorization>> FindBySubject(Subject subject, CancellationToken cancellationToken = default)
    {
        await using var scope = await database.Cluster();
        return (await scope.DbContext.Authorizations.Where(a => a.Subject == subject.Value).ToListAsync(cancellationToken)).Select(ToAuthorization).ToArray();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Authorization>> FindByApplicationIdAndSubject(ApplicationId applicationId, Subject subject, CancellationToken cancellationToken = default)
    {
        await using var scope = await database.Cluster();
        return (await scope.DbContext.Authorizations.Where(a => a.ApplicationId == applicationId.Value && a.Subject == subject.Value).ToListAsync(cancellationToken)).Select(ToAuthorization).ToArray();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Authorization>> FindByApplicationIdSubjectAndStatus(ApplicationId applicationId, Subject subject, AuthorizationStatus status, CancellationToken cancellationToken = default)
    {
        await using var scope = await database.Cluster();
        return (await scope.DbContext.Authorizations.Where(a => a.ApplicationId == applicationId.Value && a.Subject == subject.Value && a.Status == status.Value).ToListAsync(cancellationToken)).Select(ToAuthorization).ToArray();
    }

    /// <inheritdoc/>
    public async Task<long> Prune(DateTimeOffset threshold, CancellationToken cancellationToken = default)
    {
        await using var scope = await database.Cluster();
        var expired = await scope.DbContext.Authorizations.Where(a => a.CreationDate != null && a.CreationDate < threshold).ToListAsync(cancellationToken);
        scope.DbContext.Authorizations.RemoveRange(expired);
        await scope.DbContext.SaveChangesAsync(cancellationToken);
        return expired.Count;
    }

    static AuthorizationEntity ToEntity(Authorization authorization) => new()
    {
        Id = authorization.Id.Value,
        ApplicationId = authorization.ApplicationId?.Value,
        Subject = authorization.Subject?.Value,
        Type = authorization.Type?.ToString(),
        Status = authorization.Status?.Value,
        CreationDate = authorization.CreationDate
    };

    static void UpdateFrom(AuthorizationEntity entity, Authorization authorization)
    {
        entity.ApplicationId = authorization.ApplicationId?.Value;
        entity.Subject = authorization.Subject?.Value;
        entity.Type = authorization.Type?.ToString();
        entity.Status = authorization.Status?.Value;
        entity.CreationDate = authorization.CreationDate;
    }

    Authorization ToAuthorization(AuthorizationEntity entity)
    {
        var scopes = ImmutableArray<Concepts.Security.Scope>.Empty;
        if (!string.IsNullOrEmpty(entity.Scopes))
        {
            try
            {
                var scopeStrings = JsonSerializer.Deserialize<string[]>(entity.Scopes, jsonSerializerOptions) ?? [];
                scopes = [.. scopeStrings.Select(s => (Concepts.Security.Scope)s)];
            }
            catch
            {
                // Ignore deserialization errors
            }
        }

        var properties = ImmutableDictionary<PropertyName, JsonElement>.Empty;
        if (!string.IsNullOrEmpty(entity.Properties))
        {
            try
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(entity.Properties, jsonSerializerOptions);
                if (dict is not null)
                {
                    properties = dict.ToImmutableDictionary(k => (PropertyName)k.Key, v => v.Value);
                }
            }
            catch
            {
                // Ignore deserialization errors
            }
        }

        return new Authorization(
            (AuthorizationId)entity.Id,
            entity.ApplicationId.HasValue ? (ApplicationId)entity.ApplicationId.Value : null,
            entity.Subject is not null ? (Subject)entity.Subject : null,
            entity.Type is not null ? Enum.Parse<AuthorizationType>(entity.Type) : null,
            entity.Status is not null ? (AuthorizationStatus)entity.Status : null,
            scopes,
            entity.CreationDate,
            properties);
    }
}
