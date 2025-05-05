// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Represents the startup class for the integration tests.
/// </summary>
public class Startup
{
    /// <summary>
    /// Configures the application.
    /// </summary>
    /// <param name="app"><see cref="IApplicationBuilder"/> to configure.</param>
    public void Configure(IApplicationBuilder app)
    {
        app.UseCratisChronicle();
    }
}
