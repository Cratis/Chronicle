// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Chronicle.Storage.Security;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Cluster.Security;

/// <summary>
/// SQL implementation of <see cref="ITokenStorage"/>.
/// </summary>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
/// <param name="jsonSerializerOptions">The configured <see cref="JsonSerializerOptions"/>.</param>
public class SqlTokenStorage(IDatabase database, JsonSerializerOptions jsonSerializerOptions) : ITokenStorage
{
    /// <inheritdoc/>
    public async Task<Token?> GetById(string id, CancellationToken cancellationToken = default)
    {
        await using var scope = await database.Cluster();
        var entity = await scope.DbContext.Tokens.FindAsync([id], cancellationToken);
        return entity is null ? null : ToToken(entity);
    }

    /// <inheritdoc/>
    public async Task<Token?> GetByReferenceId(string referenceId, CancellationToken cancellationToken = default)
    {
        await using var scope = await database.Cluster();
        var entity = await scope.DbContext.Tokens.FirstOrDefaultAsync(t => t.ReferenceId == referenceId, cancellationToken);
        return entity is null ? null : ToToken(entity);
    }

    /// <inheritdoc/>
    public async Task Create(Token token, CancellationToken cancellationToken = default)
    {
        await using var scope = await database.Cluster();
        scope.DbContext.Tokens.Add(ToEntity(token));
        await scope.DbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task Update(Token token, CancellationToken cancellationToken = default)
    {
        await using var scope = await database.Cluster();
        var entity = await scope.DbContext.Tokens.FindAsync([token.Id], cancellationToken);
        if (entity is not null)
        {
            UpdateFrom(entity, token);
            await scope.DbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            scope.DbContext.Tokens.Add(ToEntity(token));
            await scope.DbContext.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task Delete(string id, CancellationToken cancellationToken = default)
    {
        await using var scope = await database.Cluster();
        var entity = await scope.DbContext.Tokens.FindAsync([id], cancellationToken);
        if (entity is not null)
        {
            scope.DbContext.Tokens.Remove(entity);
            await scope.DbContext.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task<long> Count(CancellationToken cancellationToken = default)
    {
        await using var scope = await database.Cluster();
        return await scope.DbContext.Tokens.LongCountAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Token>> List(int? count, int? offset, CancellationToken cancellationToken = default)
    {
        await using var scope = await database.Cluster();
        var query = scope.DbContext.Tokens.AsQueryable();
        if (offset.HasValue)
        {
            query = query.Skip(offset.Value);
        }

        if (count.HasValue)
        {
            query = query.Take(count.Value);
        }

        return (await query.ToListAsync(cancellationToken)).Select(ToToken).ToArray();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Token>> FindByApplicationId(string applicationId, CancellationToken cancellationToken = default)
    {
        await using var scope = await database.Cluster();
        return (await scope.DbContext.Tokens.Where(t => t.ApplicationId == applicationId).ToListAsync(cancellationToken)).Select(ToToken).ToArray();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Token>> FindByAuthorizationId(string authorizationId, CancellationToken cancellationToken = default)
    {
        await using var scope = await database.Cluster();
        return (await scope.DbContext.Tokens.Where(t => t.AuthorizationId == authorizationId).ToListAsync(cancellationToken)).Select(ToToken).ToArray();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Token>> FindBySubject(string subject, CancellationToken cancellationToken = default)
    {
        await using var scope = await database.Cluster();
        return (await scope.DbContext.Tokens.Where(t => t.Subject == subject).ToListAsync(cancellationToken)).Select(ToToken).ToArray();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Token>> FindByApplicationIdAndSubject(string applicationId, string subject, CancellationToken cancellationToken = default)
    {
        await using var scope = await database.Cluster();
        return (await scope.DbContext.Tokens.Where(t => t.ApplicationId == applicationId && t.Subject == subject).ToListAsync(cancellationToken)).Select(ToToken).ToArray();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Token>> FindByApplicationIdSubjectAndStatus(string applicationId, string subject, string status, CancellationToken cancellationToken = default)
    {
        await using var scope = await database.Cluster();
        return (await scope.DbContext.Tokens.Where(t => t.ApplicationId == applicationId && t.Subject == subject && t.Status == status).ToListAsync(cancellationToken)).Select(ToToken).ToArray();
    }

    /// <inheritdoc/>
    public async Task<long> Prune(DateTimeOffset threshold, CancellationToken cancellationToken = default)
    {
        await using var scope = await database.Cluster();
        var expired = await scope.DbContext.Tokens.Where(t => t.CreationDate != null && t.CreationDate < threshold).ToListAsync(cancellationToken);
        scope.DbContext.Tokens.RemoveRange(expired);
        await scope.DbContext.SaveChangesAsync(cancellationToken);
        return expired.Count;
    }

    static TokenEntity ToEntity(Token token) => new()
    {
        Id = token.Id,
        ApplicationId = token.ApplicationId,
        AuthorizationId = token.AuthorizationId,
        Subject = token.Subject,
        Type = token.Type,
        Status = token.Status,
        Payload = token.Payload,
        ReferenceId = token.ReferenceId,
        CreationDate = token.CreationDate,
        ExpirationDate = token.ExpirationDate,
        RedemptionDate = token.RedemptionDate,
    };

    static void UpdateFrom(TokenEntity entity, Token token)
    {
        entity.ApplicationId = token.ApplicationId;
        entity.AuthorizationId = token.AuthorizationId;
        entity.Subject = token.Subject;
        entity.Type = token.Type;
        entity.Status = token.Status;
        entity.Payload = token.Payload;
        entity.ReferenceId = token.ReferenceId;
        entity.CreationDate = token.CreationDate;
        entity.ExpirationDate = token.ExpirationDate;
        entity.RedemptionDate = token.RedemptionDate;
    }

    Token ToToken(TokenEntity entity)
    {
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

        return new Token
        {
            Id = entity.Id,
            ApplicationId = entity.ApplicationId,
            AuthorizationId = entity.AuthorizationId,
            Subject = entity.Subject,
            Type = entity.Type,
            Status = entity.Status,
            Payload = entity.Payload,
            ReferenceId = entity.ReferenceId,
            CreationDate = entity.CreationDate,
            ExpirationDate = entity.ExpirationDate,
            RedemptionDate = entity.RedemptionDate,
            Properties = properties
        };
    }
}
