// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Api.given;

/// <summary>
/// Base specification for API integration tests.
/// </summary>
/// <remarks>
/// Overrides disposal to skip the base fixture's <c language="csharp">RemoveAllDatabases()</c> call - dropping
/// every MongoDB database between specs wipes the Chronicle cluster's <c language="csharp">event-stores</c>
/// collection, including the System event store registered once when the container starts.
/// The already-activated namespace grain never rewrites it, so the next spec to query event
/// stores would find System missing. These specs use unique event-store names, so cleanup
/// between them is unnecessary.
/// </remarks>
/// <param name="fixture">The <see cref="ChronicleOutOfProcessFixtureWithLocalImage"/> fixture.</param>
public class Specification(ChronicleOutOfProcessFixtureWithLocalImage fixture) : Specification<ChronicleOutOfProcessFixtureWithLocalImage, ApiWebApplicationFactory, Program>(fixture)
{
    /// <inheritdoc/>
#pragma warning disable CA2215 // Skipping base.DisposeAsync() intentionally - see remarks above.
    public override Task DisposeAsync() => OnDisposeAsync();
#pragma warning restore CA2215
}
