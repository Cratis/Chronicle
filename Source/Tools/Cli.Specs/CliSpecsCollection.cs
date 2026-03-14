// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Cli;

/// <summary>
/// Defines the xUnit test collection for CLI specs that share global state (environment variables, Console).
/// </summary>
[CollectionDefinition(Name)]
public static class CliSpecsCollection
{
    /// <summary>
    /// The name of the collection.
    /// </summary>
    public const string Name = "CliSpecs";
}
