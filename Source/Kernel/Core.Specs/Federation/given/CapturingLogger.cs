// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Federation.given;

/// <summary>
/// An <see cref="ILogger{TCategoryName}"/> that keeps every message it was asked to write, with the values of its structured state.
/// </summary>
/// <typeparam name="T">The category.</typeparam>
public class CapturingLogger<T> : ILogger<T>
{
    /// <summary>
    /// Gets everything that was logged, including exception messages and structured values.
    /// </summary>
    public List<string> Entries { get; } = [];

    /// <summary>
    /// Check whether anything logged contains a value.
    /// </summary>
    /// <param name="value">The value to look for.</param>
    /// <returns>True when something logged contains it.</returns>
    public bool Contains(string value) => Entries.Exists(_ => _.Contains(value, StringComparison.Ordinal));

    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc/>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var values = state is IEnumerable<KeyValuePair<string, object?>> pairs ? string.Join(' ', pairs.Select(_ => $"{_.Key}={_.Value}")) : string.Empty;
        Entries.Add($"{formatter(state, exception)} {values} {exception}");
    }
}
