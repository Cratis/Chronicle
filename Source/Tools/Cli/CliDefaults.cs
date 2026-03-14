// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Cli;

/// <summary>
/// Default values used across the CLI.
/// </summary>
public static class CliDefaults
{
    /// <summary>
    /// The default event sequence identifier.
    /// </summary>
    public const string DefaultEventSequenceId = "event-log";

    /// <summary>
    /// The default event store name.
    /// </summary>
    public const string DefaultEventStoreName = "default";

    /// <summary>
    /// The default namespace name.
    /// </summary>
    public const string DefaultNamespaceName = "Default";

    /// <summary>
    /// The default management port.
    /// </summary>
    public const int DefaultManagementPort = 8080;

    /// <summary>
    /// Environment variable name for the Chronicle connection string.
    /// </summary>
    public const string ConnectionStringEnvVar = "CHRONICLE_CONNECTION_STRING";

    /// <summary>
    /// Environment variable name for the management port override.
    /// </summary>
    public const string ManagementPortEnvVar = "CHRONICLE_MANAGEMENT_PORT";

    /// <summary>
    /// Standard error message when the CLI cannot reach the Chronicle server.
    /// </summary>
    public const string CannotConnectMessage = "Cannot connect to Chronicle server";
}
