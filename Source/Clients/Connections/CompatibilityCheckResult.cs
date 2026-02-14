// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Represents the result of a compatibility check.
/// </summary>
/// <param name="IsCompatible">Whether the client is compatible with the server.</param>
/// <param name="Errors">List of incompatibility errors, if any.</param>
public record CompatibilityCheckResult(bool IsCompatible, IEnumerable<string> Errors);
