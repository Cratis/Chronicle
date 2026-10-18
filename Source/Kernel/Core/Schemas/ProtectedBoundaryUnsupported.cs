// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// The exception that is thrown when a protected boundary cannot report an unambiguous key location.
/// </summary>
/// <param name="path">The unsupported boundary path.</param>
public class ProtectedBoundaryUnsupported(string path) : Exception($"Protected boundary at '{path}' is unsupported.")
{
    /// <summary>
    /// Gets the unsupported boundary path.
    /// </summary>
    public string Path { get; } = path;
}
