// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Specifications;

/// <summary>
/// Represents a null logger scope.
/// </summary>
public class NullLoggerScope : IDisposable
{
    /// <summary>
    /// Gets the singleton instance of <see cref="NullLoggerScope"/>.
    /// </summary>
    public static readonly NullLoggerScope Instance = new();

    /// <inheritdoc/>
    public void Dispose()
    {
    }
}
