// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.XUnit;

/// <summary>
/// Represents an implementation of <see cref="ILoggerFactory"/> that can create <see cref="NullLogger"/> loggers.
/// </summary>
public class NullLoggerFactory : ILoggerFactory
{
    /// <inheritdoc/>
    public void AddProvider(ILoggerProvider provider)
    {
    }

    /// <inheritdoc/>
    public ILogger CreateLogger(string categoryName) => NullLogger.Instance;

    /// <inheritdoc/>
    public void Dispose()
    {
    }
}
