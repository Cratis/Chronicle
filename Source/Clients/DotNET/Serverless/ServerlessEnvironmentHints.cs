// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Serverless;

/// <summary>
/// Detects hosts that suggest the client should run in serverless mode.
/// </summary>
internal static class ServerlessEnvironmentHints
{
    static readonly string[] _variables =
    [
        "AWS_LAMBDA_FUNCTION_NAME",
        "FUNCTIONS_WORKER_RUNTIME",
        "K_SERVICE",
        "CONTAINER_APP_NAME"
    ];

    /// <summary>
    /// Detects a function host from its environment variables.
    /// </summary>
    /// <param name="getVariable">Reads an environment variable by name.</param>
    /// <returns>The name of the first variable that is set, or null when none is.</returns>
    internal static string? Detect(Func<string, string?> getVariable) =>
        _variables.FirstOrDefault(_ => !string.IsNullOrEmpty(getVariable(_)));
}
