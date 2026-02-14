// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Represents the result of a compatibility check.
/// </summary>
/// <param name="Errors">List of incompatibility errors, if any.</param>
public record CompatibilityCheckResult(IEnumerable<string> Errors)
{
    /// <summary>
    /// Gets the materialized list of errors.
    /// </summary>
    public IEnumerable<string> Errors { get; } = Errors.ToList();

    /// <summary>
    /// Gets a value indicating whether the client is compatible with the server.
    /// </summary>
    public bool IsCompatible => !Errors.Any();
}
