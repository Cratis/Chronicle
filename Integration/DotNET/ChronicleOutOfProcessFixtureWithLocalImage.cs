// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Builders;

namespace Cratis.Chronicle.Integration;

/// <summary>
/// Represents a fixture for the Chronicle Out of Process integration tests using a locally built image.
/// </summary>
public class ChronicleOutOfProcessFixtureWithLocalImage : ChronicleOutOfProcessFixture
{
    /// <inheritdoc/>
    protected override ContainerBuilder ConfigureImage(ContainerBuilder builder) =>
        builder.WithImage(Environment.GetEnvironmentVariable("CRATIS_CHRONICLE_LOCAL_IMAGE") ?? "cratis/chronicle:local-development");
}
