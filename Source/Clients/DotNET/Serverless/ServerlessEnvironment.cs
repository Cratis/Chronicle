// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.Serverless;

/// <summary>
/// Resolves the name of the hosting environment for the serverless checks.
/// </summary>
internal static class ServerlessEnvironment
{
    const string DevelopmentName = "Development";
    const string ProductionName = "Production";

    /// <summary>
    /// Resolves the environment name from the host, falling back to the process environment variables.
    /// </summary>
    /// <param name="provider">The <see cref="IServiceProvider"/> of the client.</param>
    /// <returns>The environment name.</returns>
    internal static string Resolve(IServiceProvider provider) => Resolve(provider, Environment.GetEnvironmentVariable);

    /// <summary>
    /// Resolves the environment name from the host, falling back to the given environment variables.
    /// </summary>
    /// <param name="provider">The <see cref="IServiceProvider"/> of the client.</param>
    /// <param name="getVariable">Reads an environment variable by name.</param>
    /// <returns>The environment name.</returns>
    internal static string Resolve(IServiceProvider provider, Func<string, string?> getVariable)
    {
        // The default provider activates anything it is asked for and throws for interfaces, so it is never asked.
        if (provider is not DefaultServiceProvider && provider.GetService(typeof(IHostEnvironment)) is IHostEnvironment environment)
        {
            return environment.EnvironmentName;
        }

        var name = getVariable("DOTNET_ENVIRONMENT");
        if (string.IsNullOrWhiteSpace(name))
        {
            name = getVariable("ASPNETCORE_ENVIRONMENT");
        }

        return string.IsNullOrWhiteSpace(name) ? ProductionName : name;
    }

    /// <summary>
    /// Gets whether the environment name is the Development environment.
    /// </summary>
    /// <param name="environmentName">The environment name.</param>
    /// <returns>True if it is Development, false otherwise.</returns>
    internal static bool IsDevelopment(string environmentName) => string.Equals(environmentName, DevelopmentName, StringComparison.OrdinalIgnoreCase);
}
