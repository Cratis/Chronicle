// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Users;

/// <summary>
/// Represents the API for user queries.
/// </summary>
/// <param name="client">The <see cref="IChronicleClient"/>.</param>
[Route("/api/users")]
public class UserQueries(IChronicleClient client) : ControllerBase
{
    /// <summary>
    /// Check if the admin user needs configuration.
    /// </summary>
    /// <returns>True if admin needs configuration - always returns true for now as we need to implement proper read model querying.</returns>
    [HttpGet("admin/needs-configuration")]
    public Task<bool> AdminNeedsConfiguration()
    {
        // TODO: Implement proper read model querying when gRPC contracts are in place
        // For now, we return true to indicate the admin needs configuration
        return Task.FromResult(true);
    }
}
