// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Cli.Commands.LlmContext;

/// <summary>
/// Describes how the CLI connects to a Chronicle server.
/// </summary>
public record ConnectionInfoDescriptor
{
    /// <summary>
    /// Gets or sets the default connection string.
    /// </summary>
    public required string DefaultConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the environment variable name for connection string override.
    /// </summary>
    public required string EnvironmentVariable { get; set; }

    /// <summary>
    /// Gets or sets the configuration file path.
    /// </summary>
    public required string ConfigFile { get; set; }

    /// <summary>
    /// Gets or sets the precedence order for connection string resolution.
    /// </summary>
    public required IReadOnlyList<string> Precedence { get; set; }
}
