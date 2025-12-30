// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Testing;

#pragma warning disable SA1402 // File may only contain a single type

/// <summary>
/// Represents a null logger.
/// </summary>
public class NullLogger : ILogger
{
    /// <summary>
    /// Gets the instance of the <see cref="NullLogger"/>.
    /// </summary>
    public static readonly ILogger Instance = new NullLogger();

    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
        => NullScope.Instance;

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) => false;

    /// <inheritdoc/>
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
    }

    sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}

/// <summary>
/// Represents a generic null logger.
/// </summary>
/// <typeparam name="T">Type to log for.</typeparam>
public class NullLogger<T> : NullLogger, ILogger<T>
{
    /// <summary>
    /// Gets the instance of the <see cref="NullLogger"/>.
    /// </summary>
    public static new readonly ILogger<T> Instance = new NullLogger<T>();
}
