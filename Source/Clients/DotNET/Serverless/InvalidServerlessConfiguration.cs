// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Cratis.Chronicle.Serverless;

/// <summary>
/// Exception that gets thrown when the serverless configuration is not valid.
/// </summary>
/// <param name="failures">The reasons the configuration is not valid.</param>
[Experimental("CHRX0001", UrlFormat = "https://www.cratis.io/docs/chronicle/clients/dotnet/serverless-mode")]
public class InvalidServerlessConfiguration(IEnumerable<string> failures)
    : Exception(ServerlessConfigurationValidation.Format(failures))
{
    /// <summary>
    /// Gets the reasons the configuration is not valid.
    /// </summary>
    public IReadOnlyList<string> Failures { get; } = failures.ToArray();
}
