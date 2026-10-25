// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Serverless;

/// <summary>
/// Exception that gets thrown when the serverless configuration is not valid.
/// </summary>
/// <param name="failures">The reasons the configuration is not valid.</param>
public class InvalidServerlessConfiguration(IEnumerable<string> failures)
    : Exception(ServerlessConfigurationValidation.Format(failures))
{
    /// <summary>
    /// Gets the reasons the configuration is not valid.
    /// </summary>
    public IReadOnlyList<string> Failures { get; } = failures.ToArray();
}
