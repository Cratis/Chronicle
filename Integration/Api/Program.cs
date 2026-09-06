// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Chronicle.Integration.Api;

/// <summary>
/// Entry point for the Chronicle HTTP API integration tests.
/// </summary>
/// <remarks>
/// The specs talk to a containerized kernel over HTTP, so this host exists only to give
/// <c>WebApplicationFactory</c> something to start - it serves nothing itself.
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
    /// Main entry point.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>A task representing the async operation.</returns>
    public static async Task Main(string[] args)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();
        await app.RunAsync();
    }
}
