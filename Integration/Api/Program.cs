// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Chronicle.Integration.Api;

/// <summary>
/// Entry point for the Chronicle HTTP API integration tests.
/// </summary>
/// <remarks>
/// The specs talk to a containerized kernel over HTTP, so this host exists only to give
/// <c language="csharp">WebApplicationFactory</c> something to start - it serves nothing itself.
/// </remarks>
public sealed class Program
{
    /// <summary>
    /// Private constructor to prevent external instantiation.
    /// This class is used as a marker for WebApplicationFactory.
    /// </summary>
    Program()
    {
    }

    /// <summary>
    /// Creates the marker host independently of the xUnit executable entry point.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>The marker host builder.</returns>
    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        return Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(builder => builder.Configure(_ => { }));
    }
}
