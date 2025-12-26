// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Security;

/// <summary>
/// Extension methods for converting between contract and API application types.
/// </summary>
public static class ApplicationConverters
{
    /// <summary>
    /// Convert from contract application to API application.
    /// </summary>
    /// <param name="client">Contract application.</param>
    /// <returns>API application.</returns>
    public static Application ToApi(this Contracts.Security.Application client) => new(
        client.Id,
        client.ClientId,
        client.IsActive,
        client.CreatedAt,
        client.LastModifiedAt);

    /// <summary>
    /// Convert from multiple contract application to API application.
    /// </summary>
    /// <param name="clients">Contract application.</param>
    /// <returns>API application.</returns>
    public static IEnumerable<Application> ToApi(this IEnumerable<Contracts.Security.Application> clients) =>
        clients.Select(ToApi);
}
