// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Workbench;

namespace WorkbenchHost;

/// <summary>
/// A standalone Workbench host connecting to Chronicle over gRPC. The connection string can hold
/// multiple servers (or a chronicle+srv address) - the connection load balances across them,
/// which is what this host demonstrates.
/// </summary>
public static class Program
{
    /// <summary>
    /// The entry point.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var connectionString = builder.Configuration["Cratis:Chronicle:ConnectionString"] is { } configured
            ? new ChronicleConnectionString(configured)
            : ChronicleConnectionString.Development;

        builder.Services.AddCratisChronicleConnection(connectionString);
        builder.UseCratisChronicleWorkbench();

        var app = builder.Build();
        await app.RunAsync();
    }
}
