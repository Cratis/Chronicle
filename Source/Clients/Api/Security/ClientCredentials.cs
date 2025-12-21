// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Security;

namespace Cratis.Chronicle.Api.Security;

/// <summary>
/// Represents client credentials in the Chronicle system.
/// </summary>
public record ClientCredentials(string Id, string ClientId, bool IsActive, DateTimeOffset CreatedAt, DateTimeOffset? LastModifiedAt);

/// <summary>
/// Extension methods for converting between contract and API client credentials types.
/// </summary>
public static class ClientCredentialsConverters
{
    /// <summary>
    /// Convert from contract client credentials to API client credentials.
    /// </summary>
    /// <param name="client">Contract client credentials.</param>
    /// <returns>API client credentials.</returns>
    public static ClientCredentials ToApi(this Contracts.Security.ClientCredentials client) => new(
        client.Id,
        client.ClientId,
        client.IsActive,
        client.CreatedAt,
        client.LastModifiedAt);

    /// <summary>
    /// Convert from multiple contract client credentials to API client credentials.
    /// </summary>
    /// <param name="clients">Contract client credentials.</param>
    /// <returns>API client credentials.</returns>
    public static IEnumerable<ClientCredentials> ToApi(this IEnumerable<Contracts.Security.ClientCredentials> clients) =>
        clients.Select(ToApi);
}
