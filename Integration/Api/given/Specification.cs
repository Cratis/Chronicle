// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Api.given;

/// <summary>
/// Base specification for API integration tests. Overrides disposal to skip
/// <c>RemoveAllDatabases()</c> — dropping all MongoDB databases between tests
/// wipes the OpenIddict auth state (applications, tokens) and the Chronicle
/// <c>event-stores</c> collection while the Docker container's in-memory cache
/// remains stale. Tests use unique event-store names so cleanup is unnecessary.
/// </summary>
/// <param name="fixture">The <see cref="ChronicleOutOfProcessFixtureWithLocalImage"/> fixture.</param>
public class Specification(ChronicleOutOfProcessFixtureWithLocalImage fixture) : Specification<ChronicleOutOfProcessFixtureWithLocalImage, ApiWebApplicationFactory, Program>(fixture)
{
    /// <inheritdoc/>
#pragma warning disable CA2215 // Intentionally not calling base — base calls RemoveAllDatabases() which wipes OpenIddict state
    public override Task DisposeAsync()
#pragma warning restore CA2215
    {
        ChronicleFixture.PerformBackup(GetType().FullName ?? GetType().Name);
        return Task.CompletedTask;
    }
}
