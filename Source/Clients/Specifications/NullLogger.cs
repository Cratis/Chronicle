// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Specifications;

/// <summary>
/// Represents a <see cref="ILogger{T}"/> that doesn't do any logging.
/// </summary>
/// <typeparam name="T">Type the logger is for.</typeparam>
public class NullLogger<T> : ILogger<T>
{
    /// <inheritdoc/>
    public IDisposable BeginScope<TState>(TState state) => throw new NotImplementedException();

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) => false;

    /// <inheritdoc/>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
    }
}
