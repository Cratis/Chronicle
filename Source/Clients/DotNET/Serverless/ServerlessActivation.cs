// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.Serverless;

/// <summary>
/// The single branching point that turns a client into a serverless client.
/// </summary>
internal static class ServerlessActivation
{
    /// <summary>
    /// Gets whether serverless mode is on, through the options or the connection string.
    /// </summary>
    /// <param name="options">The <see cref="ChronicleOptions"/> to check.</param>
    /// <returns>True if serverless mode is on, false otherwise.</returns>
    internal static bool IsOn(ChronicleOptions options) =>
        options.Serverless.Mode == ServerlessMode.Serverless || options.ConnectionString.Serverless;

    /// <summary>
    /// Validates the options and applies the short-lived behavior serverless mode implies. Does nothing when the mode is off.
    /// </summary>
    /// <param name="options">The <see cref="ChronicleOptions"/> to apply to.</param>
    /// <param name="environmentName">The name of the hosting environment.</param>
    /// <exception cref="InvalidServerlessConfiguration">Thrown when the serverless configuration is not valid.</exception>
    internal static void Apply(ChronicleOptions options, string environmentName)
    {
        if (!IsOn(options))
        {
            return;
        }

        var failures = ServerlessConfigurationValidation.Validate(options, environmentName);
        if (failures.Count > 0)
        {
            throw new InvalidServerlessConfiguration(failures);
        }

        options.SkipKeepAlive = true;
        options.RegistrationMode = options.Serverless.Registration == ServerlessRegistration.Deploy
            ? RegistrationMode.AtDeployment
            : RegistrationMode.Inline;
    }

    /// <summary>
    /// Requires an injected connection to skip keep-alive.
    /// </summary>
    /// <param name="connection">The injected <see cref="IChronicleConnection"/>.</param>
    /// <exception cref="InvalidServerlessConfiguration">Thrown when the connection keeps alive.</exception>
    internal static void RequireSkippedKeepAlive(IChronicleConnection connection)
    {
        if (connection is not IConnectionKeepAlive { IsSkipped: true })
        {
            throw new InvalidServerlessConfiguration(["Serverless mode requires a connection that skips keep-alive, but the injected connection keeps alive."]);
        }
    }
}
