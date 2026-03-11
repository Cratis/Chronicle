// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Cli;

/// <summary>
/// Defines standard exit codes returned by CLI commands.
/// </summary>
public static class ExitCodes
{
    /// <summary>
    /// The command completed successfully.
    /// </summary>
    public const int Success = 0;

    /// <summary>
    /// The command failed due to invalid input or a resource was not found.
    /// </summary>
    public const int NotFound = 1;

    /// <summary>
    /// The command could not connect to the Chronicle server.
    /// </summary>
    public const int ConnectionError = 2;

    /// <summary>
    /// The server returned an unexpected error.
    /// </summary>
    public const int ServerError = 3;

    /// <summary>
    /// Authentication failed or credentials are missing.
    /// </summary>
    public const int AuthenticationError = 4;
}
