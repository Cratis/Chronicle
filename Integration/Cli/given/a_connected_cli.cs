// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.Integration.Cli.given;

/// <summary>
/// Base context for specs that run CLI commands against a live Chronicle server.
/// </summary>
public class a_connected_cli : Specification
{
    /// <summary>
    /// Gets the gRPC connection string for the Docker-hosted Chronicle server.
    /// </summary>
    protected static string ConnectionString
    {
        get
        {
            var certPath = Uri.EscapeDataString(ChronicleOutOfProcessFixtureWithLocalImage.CertificatePath);
            return $"chronicle://{ChronicleConnectionString.DevelopmentClient}:{ChronicleConnectionString.DevelopmentClientSecret}@localhost:35001/?certificatePath={certPath}&certificatePassword=TestPassword123";
        }
    }

    /// <summary>
    /// Runs a CLI command against the live server with JSON output format.
    /// </summary>
    /// <param name="args">The command arguments (without --server, --management-port, and --output flags).</param>
    /// <returns>The command execution result.</returns>
    protected static Task<CliCommandResult> RunCliAsync(params string[] args)
    {
        var allArgs = new List<string>(args) { "--server", ConnectionString, "--management-port", "8081", "--output", "json" };
        return CliCommandRunner.RunAsync([.. allArgs]);
    }
}
